using Api.Contexts;
using Api.Startup;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Uploader;

public partial class AutoService
{
    /// <summary>
    /// Find active media refs 
    /// </summary>
    /// <returns></returns>
    public virtual ValueTask<List<Upload>> ActiveRefs(Context context, Dictionary<uint, int> usageMap)
    {
        // This service doesn't have a content type thus can just safely do nothing at all.
        return new ValueTask<List<Upload>>((List<Upload>)null);
    }

    /// <summary>
    /// Update any ref fields (ignoring canvas for the moment) such that the ref contains the full ref value including width, focal point etc.
    /// </summary>
    /// <returns></returns>
    public virtual ValueTask<List<MediaRef>> UpdateRefs(Context context, bool SaveChanges, Dictionary<uint, string> refMap)
    {
        // This service doesn't have a content type thus can just safely do nothing at all.
        return new ValueTask<List<MediaRef>>((List<MediaRef>)null);
    }

    /// <summary>
    /// Replace media refs 
    /// </summary>
    /// <returns></returns>
    public virtual ValueTask<List<MediaRef>> ReplaceRefs(Context context, string sourceRef, string targetRef)
    {
        // This service doesn't have a content type thus can just safely do nothing at all.
        return new ValueTask<List<MediaRef>>((List<MediaRef>)null);
    }

}
public partial class AutoService<T, ID>
{

    /// <summary>
    /// Update any data ref fields (ignoring canvas for the moment) such that the ref contains the full ref value including width, focal point etc.
    /// </summary>
    /// <returns></returns>
    public override async ValueTask<List<MediaRef>> UpdateRefs(Context context, bool SaveChanges, Dictionary<uint, string> refMap)
    {
        List<MediaRef> mediaRefs = new List<MediaRef>();

        var refFields = new List<JsonField>();

        JsonField idField = null;
        JsonField nameField = null;
        JsonField descField = null;

        // Get the field info:
        var fieldInfo = await GetJsonStructure(context);

        // find any fields, need to check them all for ref formatted data
        foreach (var fieldPair in fieldInfo.AllFields)
        {
            // keep track of any strings as they may contain a ref, will check actual values later
            if (fieldPair.Value.FieldInfo != null && fieldPair.Value.TargetType == typeof(string))
            {
                refFields.Add(fieldPair.Value);
            }

            if (fieldPair.Key == "id")
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

        if (!refFields.Any())
        {
            return mediaRefs;
        }

        // find any content which has populated media refs
        var filter = Where();

        var objectsWithRefs = await filter.ListAll(context);

        if (objectsWithRefs == null || !objectsWithRefs.Any())
        {
            return mediaRefs;
        }

        // Check each object
        foreach (var objectWithRefs in objectsWithRefs)
        {
            foreach (var field in refFields)
            {
                var uploadRef = field.FieldInfo.GetValue(objectWithRefs) as string;

                // does the field value contain a valid ref 
                var uploadId = GetImageRefId(uploadRef);
                if (uploadId == 0)
                {
                    continue;
                }

                // does the existign upload/media id still exist ? 
                if (!refMap.TryGetValue(uploadId, out string latestRef))
                {
                    mediaRefs.Add(new MediaRef()
                    {
                        Id = (uint)idField?.FieldInfo.GetValue(objectWithRefs),
                        Type = ServicedType.Name,
                        Field = field.FieldInfo.Name,
                        Name = nameField?.FieldInfo.GetValue(objectWithRefs).ToString(),
                        Description = descField?.FieldInfo.GetValue(objectWithRefs).ToString(),
                        ExistingRef = uploadRef,
                        LocaleId = context.LocaleId,
                        Status = $"ERROR - Unable to locate current media ref for id [{uploadId}]"
                    });

                    continue;
                }

                if (uploadRef != latestRef)
                {
                    // Found a ref to upgrade!
                    mediaRefs.Add(new MediaRef()
                    {
                        Id = (uint)idField?.FieldInfo.GetValue(objectWithRefs),
                        Type = ServicedType.Name,
                        Field = field.FieldInfo.Name,
                        Name = nameField?.FieldInfo.GetValue(objectWithRefs).ToString(),
                        Description = descField?.FieldInfo.GetValue(objectWithRefs).ToString(),
                        ExistingRef = uploadRef,
                        UpdatedRef = latestRef,
                        LocaleId = context.LocaleId,
                        Status = SaveChanges ? "OK" : "PENDING UPDATE"
                    });

                    if (SaveChanges)
                    {
                        // objectWithRefs is of type T
                        await Update(context, objectWithRefs, (Context c, T obj, T orig) =>
                        {
                            field.FieldInfo.SetValue(obj, latestRef);
                        });
                    }
                }
            }
        }

        return mediaRefs;

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

        // does the source field contain a valid ref and hence id 
        var sourceId = GetImageRefId(sourceRef);
        if (sourceId == 0)
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
            // keep track of any strings as they may contain a ref, will check actual values later
            if (fieldPair.Value.FieldInfo != null && fieldPair.Value.TargetType == typeof(string))
            {
                refFields.Add(fieldPair.Value);
            }

            if (fieldPair.Key == "id")
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

        if (!refFields.Any())
        {
            return mediaRefs;
        }

        // find any content which has populated media refs
        var filter = Where();

        var objectsWithRefs = await filter.ListAll(context);

        if (objectsWithRefs == null || !objectsWithRefs.Any())
        {
            return mediaRefs;
        }

        foreach (var objectWithRefs in objectsWithRefs)
        {
            var hasUpdates = false;

            // no target supplied so just get what would be replaced (readonly mode) 
            foreach (var field in refFields)
            {
                // does the field data contain a valid ref?
                var refFieldId = GetImageRefId(field.FieldInfo.GetValue(objectWithRefs)?.ToString());
                if (refFieldId == 0)
                {
                    continue;
                }

                // cannot rely on the whole ref matching so match the upload ids
                if (refFieldId == sourceId)
                {
                    hasUpdates = true;

                    mediaRefs.Add(new MediaRef()
                    {
                        Id = (uint)idField?.FieldInfo.GetValue(objectWithRefs),
                        Type = ServicedType.Name,
                        Field = field.FieldInfo.Name,
                        Name = nameField?.FieldInfo.GetValue(objectWithRefs).ToString(),
                        Description = descField?.FieldInfo.GetValue(objectWithRefs).ToString(),
                        ExistingRef = sourceRef,
                        UpdatedRef = targetRef,
                        LocaleId = context.LocaleId,
                        Status = "OK"
                    });
                }
            }

            // if the data item has matched and we are not in preview then do the updates
            if (hasUpdates && !string.IsNullOrWhiteSpace(targetRef))
            {
                // objectWithRef is of type T
                await Update(context, objectWithRefs, (Context c, T obj, T orig) =>
                {
                    foreach (var field in refFields)
                    {
                        // does the field contain a valid ref?
                        var refFieldId = GetImageRefId(field.FieldInfo.GetValue(objectWithRefs)?.ToString());
                        if (refFieldId == 0)
                        {
                            continue;
                        }

                        // cannot rely on the whole ref matching so match the upload ids
                        if (refFieldId == sourceId)
                        {
                            field.FieldInfo.SetValue(obj, targetRef);
                        }
                    }
                });
            }
        }

        return mediaRefs;
    }

    /// <summary>
    /// Find active media refs 
    /// </summary>
    /// <returns></returns>
    public override async ValueTask<List<Upload>> ActiveRefs(Context context, Dictionary<uint, int> usageMap)
    {
        List<Upload> uploads = new List<Upload>();

        if (!DataIsPersistent)
        {
            return uploads;
        }

        var refFields = new List<JsonField>();

        // Get the field info:
        var fieldInfo = await GetJsonStructure(context);

        // find any media refs 
        foreach (var fieldPair in fieldInfo.AllFields)
        {
            // keep track of any strings as they may contain a ref, will check actual values later
            if (fieldPair.Value.FieldInfo != null && fieldPair.Value.TargetType == typeof(string))
            {
                refFields.Add(fieldPair.Value);
            }
        }

        if (!refFields.Any())
        {
            return uploads;
        }

        // find any content which has populated media refs
        var filter = Where();

        var objectsWithRefs = await filter.ListAll(context);

        if (objectsWithRefs == null || !objectsWithRefs.Any())
        {
            return uploads;
        }

        var uploadService = Services.Get<UploadService>();

        // Check each object
        foreach (var objectWithRefs in objectsWithRefs)
        {
            foreach (var field in refFields)
            {
                var uploadRef = field.FieldInfo.GetValue(objectWithRefs) as string;

                var uploadId = GetImageRefId(uploadRef);
                if (uploadId == 0)
                {
                    continue;
                }

                // finally we have an active ref in use
                if (!usageMap.ContainsKey(uploadId))
                {
                    usageMap.Add(uploadId, 1);
                    var upload = await uploadService.Get(context, uploadId);
                    if (upload != null)
                    {
                        uploads.Add(upload);
                    }
                }
                else
                {
                    usageMap[uploadId]++;
                }
            }
        }
        return uploads;
    }

	/// <summary>
	/// Extract the upload id from the image ref
	/// e.g. 1840 or 77
	/// public:5A33D1474A94741A998AA72B8C722C9B/1840.jpg|webp?w=2460&amp;h=1770&amp;b=LRN8%5DCM%5E5ZI%5B%3F%5DR5xuozS6r%3D%24~of
	/// public:5A33D1474A94741A998AA72B8C722C9B/77.jpg?w=5039&amp;h = 3364
	/// </summary>
	/// <param name="uploadRef"></param>
	/// <returns></returns>

	private uint GetImageRefId(string uploadRef)
    {
        if (string.IsNullOrWhiteSpace(uploadRef) || !uploadRef.StartsWith("public:"))
        {
            return 0;
        }

        // Parse the upload ID from the ref.
        var startOfId = uploadRef.LastIndexOf('/');
        if (startOfId == -1)
        {
            return 0;
        }

        var endOfId = uploadRef.IndexOf('.', startOfId);
        if (endOfId == -1)
        {
            return 0;
        }

        var uploadIdStr = uploadRef.Substring(startOfId + 1, endOfId - startOfId - 1);
        if (uint.TryParse(uploadIdStr, out uint uploadId))
        {
            // everything seems fine
            return uploadId;
        }

        return 0;
    }
}

