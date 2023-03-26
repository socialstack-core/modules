using Api.Contexts;
using Api.Startup;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Uploader;

public partial class AutoService
{
    /// <summary>
    /// Replace media refs 
    /// </summary>
    /// <returns></returns>
    public virtual ValueTask<List<MediaRef>> ReplaceRefs(Context context, string sourceRef, string targetRef)
    {
        // This service doesn't have a content type thus can just safely do nothing at all.
        return new ValueTask<List<MediaRef>>((List<MediaRef>)null);
    }

    /// <summary>
    /// Find active media refs 
    /// </summary>
    /// <returns></returns>
    public virtual ValueTask<List<Upload>> ActiveRefs(Context context)
    {
        // This service doesn't have a content type thus can just safely do nothing at all.
        return new ValueTask<List<Upload>>((List<Upload>)null);
    }
    /// <summary>
    /// Update any ref fields (ignoring canvas for the moment) such that the ref contains the full ref value including width, focal point etc.
    /// </summary>
    /// <returns></returns>
    public virtual ValueTask UpdateRefs(Context context, Dictionary<uint, string> refMap)
    {
        // This service doesn't have a content type thus can just safely do nothing at all.
        return new ValueTask();

	}
}
public partial class AutoService<T, ID>
{
	/// <summary>
	/// Update any ref fields (ignoring canvas for the moment) such that the ref contains the full ref value including width, focal point etc.
	/// </summary>
	/// <returns></returns>
	public override async ValueTask UpdateRefs(Context context, Dictionary<uint, string> refMap)
	{
		var refFields = new List<JsonField>();

		// Get the field info:
		var fieldInfo = await GetJsonStructure(context);

		// find any media refs based on name 
		foreach (var fieldPair in fieldInfo.AllFields)
		{
			if (fieldPair.Value.FieldInfo != null && fieldPair.Value.TargetType == typeof(string))
			{
				refFields.Add(fieldPair.Value);
			}
		}

        if (!refFields.Any())
        {
            return;
        }
            
		// find any content which has populated media refs
		var filter = Where();

		var objectsWithRefs = await filter.ListAll(context);

		if (objectsWithRefs == null || !objectsWithRefs.Any())
		{
			return;
		}

		// Check each object
		foreach (var objectWithRefs in objectsWithRefs)
		{
			foreach (var field in refFields)
			{
                var uploadRef = field.FieldInfo.GetValue(objectWithRefs) as string;

                if (string.IsNullOrWhiteSpace(uploadRef) || !uploadRef.StartsWith("public:"))
                {
                    continue;
                }
                
                // Parse the upload ID from the ref.
                var startOfId = uploadRef.LastIndexOf('/');

                if (startOfId == -1)
                {
                    continue;
                }

                var endOfId = uploadRef.IndexOf('.', startOfId);

				if (endOfId == -1)
				{
					continue;
				}

                var uploadIdStr = uploadRef.Substring(startOfId + 1, endOfId - startOfId - 1);

                if (!uint.TryParse(uploadIdStr, out uint uploadId))
                {
                    continue;
                }

                if(!refMap.TryGetValue(uploadId, out string latestRef))
				{
					continue;
				}

                if (uploadRef != latestRef)
                {
                    // Found a ref to upgrade!
                    System.Console.WriteLine(uploadRef + " -> " + latestRef);
                }
			}
		}

	}

	/// <summary>
	/// Replace media refs 
	/// </summary>
	/// <returns></returns>
	public override async ValueTask<List<MediaRef>> ReplaceRefs(Context context, string sourceRef, string targetRef)
    {
        List<MediaRef> mediaRefs = new List<MediaRef>();

        if (!DataIsPersistent || string.IsNullOrWhiteSpace(sourceRef))
        {
            return mediaRefs;
        }

        var refFields = new List<JsonField>();
        
        JsonField idField = null;
        JsonField nameField = null;
        JsonField descField = null;
        
        // Get the field info:
        var fieldInfo = await GetJsonStructure(context);

        foreach (var fieldPair in fieldInfo.AllFields)
        {
            if (fieldPair.Key.EndsWith("ref") && fieldPair.Value.FieldInfo != null && fieldPair.Value.TargetType == typeof(string))
            {
                refFields.Add(fieldPair.Value);
            }

            if(fieldPair.Key == "id")
            {
                idField = fieldPair.Value;
            }

            if (fieldPair.Key == "name")
            {
                nameField = fieldPair.Value;
            }

            if (fieldPair.Key == "description")
            {
                descField = fieldPair.Value;
            }
        }

        if (refFields.Any())
        {
            // find any content which has populated media refs
            var filter = Where();
            var objectsWithThisRef = await filter.ListAll(context);

            if (objectsWithThisRef == null || !objectsWithThisRef.Any())
            {
                return mediaRefs;
            }

            foreach (var objectWithRef in objectsWithThisRef)
            {
                var hasUpdates = false;

                // no target supplied so just get what would be replaced (readonly mode) 
                foreach (var field in refFields)
                {
                    if (field.FieldInfo.GetValue(objectWithRef)?.ToString() == sourceRef)
                    {
                        hasUpdates = true;

                        mediaRefs.Add(new MediaRef()
                        {
                            Id = (uint)idField?.FieldInfo.GetValue(objectWithRef),
                            Type = ServicedType.Name,
                            Field =  field.FieldInfo.Name,
                            Name = nameField?.FieldInfo.GetValue(objectWithRef).ToString(),
                            Description = descField?.FieldInfo.GetValue(objectWithRef).ToString()
                        });
                    }
                }

                if (hasUpdates && ! string.IsNullOrWhiteSpace(targetRef))
                {
                    // objectWithRef is of type T
                    await Update(context, objectWithRef, (Context c, T obj, T orig) =>
                    {
                        foreach (var field in refFields)
                        {
                            if (field.FieldInfo.GetValue(obj)?.ToString() == sourceRef)
                            {
                                field.FieldInfo.SetValue(obj, targetRef);
                            }
                        }
                    });
                }
            }
        }
        
        return mediaRefs;
    }

    /// <summary>
    /// Find active media refs 
    /// </summary>
    /// <returns></returns>
    public override async ValueTask<List<Upload>> ActiveRefs(Context context)
    {
        List<Upload> uploads = new List<Upload>();

        if (!DataIsPersistent)
        {
            return uploads;
        }

        var refFields = new List<JsonField>();

        // Get the field info:
        var fieldInfo = await GetJsonStructure(context);

        // find any media refs based on name 
        foreach (var fieldPair in fieldInfo.AllFields)
        {
            if (fieldPair.Key.EndsWith("ref") && fieldPair.Value.FieldInfo != null && fieldPair.Value.TargetType == typeof(string))
            {
                refFields.Add(fieldPair.Value);
            }
        }

        if (refFields.Any())
        {
            // find any content which has populated media refs
            var filter = Where();

            var objectsWithRefs = await filter.ListAll(context);

            if (objectsWithRefs == null || !objectsWithRefs.Any())
            {
                return uploads;
            }

            var uploadService = Services.Get<UploadService>();
            List<string> activeRefs = new List<string>();

            //finally get any unique upload objects
            foreach (var objectWithRefs in objectsWithRefs)
            {
                foreach (var field in refFields)
                {
                    var uploadRef = field.FieldInfo.GetValue(objectWithRefs)?.ToString();

                    if (!string.IsNullOrWhiteSpace(uploadRef) && !activeRefs.Contains(uploadRef))
                    {
                        activeRefs.Add(uploadRef);
                        var upload = await uploadService.Get(context, uploadRef);
                        if (upload != null)
                        {
                            uploads.Add(upload);
                        }
                    }
                }
            }
        }

        return uploads;
    }
}

