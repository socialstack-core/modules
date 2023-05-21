using System;
using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
//using Amazon.S3.Model;
using Api.SocketServerLibrary;
using Api.Permissions;
using Api.Translate;
using Api.Eventing;
using System.IO;
using System.Text;

/// <summary>
/// A convenience controller for defining common endpoints like create, list, delete etc. Requires an AutoService of the same type to function.
/// Not required to use these - you can also just directly use ControllerBase if you want.
/// Like AutoService this isn't in a namespace due to the frequency it's used.
/// </summary>
public partial class AutoController<T, ID>
{

    /// <summary>
    ///  /// PUT /v1/entityTypeName/list.pot
    /// Update translations for this content type.
    /// </summary>
    /// <returns></returns>
    [HttpPut("list.pot")]
    public virtual async Task<object> ListPOTUpdate()
    {
        // Get context:
        var context = await Request.GetContext();

        // Admin and developer only:
        if (!context.Role.CanViewAdmin)
        {
            throw new PublicException("Admin only", "permissions", 403);
        }

        var translationService = Services.Get<TranslationService>();
        var updated = 0;
        var missing = 0;
        var skipped = 0;

        // The po data is passed via the request stream
        // have to read it all here as po parser fails when using raw request stream
        var bodyData = await new StreamReader(Request.Body, Encoding.Default).ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(bodyData))
        {
            throw new PublicException("No translation content supplied", "data");
        }

        using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(bodyData)))
        {
            var parsedPO = await translationService.ParsePOData(context, stream, _service.ServicedType);

            // By now we should have clean entries
            T previousEntry = null;
            ulong previousId = 0;

            // Get the JSON meta which will indicate exactly which fields are editable by this user (role):
            var availableFields = await _service.GetTypedJsonStructure(context);

            // For each translation in the pot file, we'll look for translations which specifically target this content type in their msgctxt.
            foreach (var po in parsedPO)
            {
                T item;
                if (previousId == po.Id)
                {
                    item = previousEntry;
                }
                else
                {
                    // Lookup the entry by its ID:
                    var itemId = _service.ConvertId(po.Id);
                    item = await _service.Get(context, itemId);
                    previousId = po.Id;
                    previousEntry = item;
                }

                if (item == null)
                {
                    // Not found by its ID.
                    // Could create here although it runs a very high chance of being wrong - if some IDs are missing then other IDs are likely incorrect.
                    missing++;
                    continue;
                }

                // Get the field metadata:
                var fieldMeta = availableFields.GetField(po.FieldName, JsonFieldGroup.Any);
                if (fieldMeta == null)
                {
                    // Content field called 'contentField' does not exist on this content type.
                    // It probably used to, so we can just ignore it.
                    continue;
                }

                if (fieldMeta.FieldInfo.GetValue(item)?.ToString() == po.Translated)
                {
                    // The value is the same 
                    skipped++;
                    continue;
                }

                // Apply the update to the object now
                await _service.Update(context, item, (Context c, T toUpdate, T orig) =>
                {
                    // item.FIELD = translatedValue;
                    fieldMeta.SetFieldValue(toUpdate, po.Translated);
                }, DataOptions.IgnorePermissions);

				Log.Info("translation", $"Updating {_service.ServicedType.Name} [{item.Id}] {fieldMeta.Name} {fieldMeta.FieldInfo.GetValue(item)?.ToString()}->{po.Translated}");

                updated++;
            }
        }

        return new
        {
            success = true,
            updated = updated,
            missing = missing,
            skipped = skipped
        };
    }

    /// <summary>
    /// GET /v1/entityTypeName/list.pot
    /// Lists all entities of this type available to this user, and outputs as a POT file.
    /// </summary>
    /// <param name="includes"></param>
    /// <param name="ignoreFields"></param>
    /// <returns></returns>
    [HttpGet("list.pot")]
    public virtual async ValueTask ListPOT([FromQuery] string includes = null, [FromQuery] string ignoreFields = null)
    {
        await ListPOT(null, includes, ignoreFields);
    }

    /// <summary>
    /// POST /v1/entityTypeName/list.pot
    /// Lists filtered entities available to this user.
    /// See the filter documentation for more details on what you can request here.
    /// </summary>
    /// <param name="filters"></param>
    /// <param name="includes"></param>
    /// <param name="ignoreFields"></param>
    /// <returns></returns>
    [HttpPost("list.pot")]
    public virtual async ValueTask ListPOT([FromBody] JObject filters, [FromQuery] string includes = null, [FromQuery] string ignoreFields = null)
    {
        var typeName = typeof(T).Name;

        var context = await Request.GetContext();

        var filter = _service.LoadFilter(filters) as Filter<T, ID>;
        filter = await _service.EventGroup.EndpointStartPotList.Dispatch(context, filter, Response);

        if (filter == null)
        {
            // A handler rejected this request
            Response.StatusCode = 404;
            return;
        }

        var results = await filter.ListAll(context);

        // For each one, output their localisable fields.
        var writer = Writer.GetPooled();
        writer.Start(null);

        // Get all fields:
        var fields = _service.GetContentFields();

        // Filter specifically to localised ones:
        var localisedFields = new List<ContentField>();

        // Exclude specified fields 
        HashSet<string> ignoreFieldList = null;
        if (!string.IsNullOrWhiteSpace(ignoreFields))
        {
            ignoreFieldList = new HashSet<string>(ignoreFields.Split(',', StringSplitOptions.RemoveEmptyEntries), StringComparer.InvariantCultureIgnoreCase);
        }

        foreach (var field in fields.List)
        {
            if (ignoreFieldList != null && ignoreFieldList.Contains(field.Name))
            {
                continue;
            }

            if (field.Localised)
            {
                localisedFields.Add(field);
            }
        }

        Response.ContentType = "text/plain";
        Response.Headers.Add("Content-Disposition", "attachment; filename=\"" + typeName + ".pot\"");

        var translationServiceConfig = Services.Get<TranslationService>().GetConfig<TranslationServiceConfig>();

        foreach (var result in results)
        {
            var id = result.Id.ToString();

            foreach (var localisedField in localisedFields)
            {
                // ID line - e.g. #: 14 - products>1>title
                writer.WriteASCII("#: ");
                writer.WriteASCII(id);
                writer.WriteASCII(">");
                writer.WriteASCII(localisedField.Name);
                writer.WriteASCII("\r\n");

                writer.WriteASCII("msgctxt \"");
                writer.WriteASCII(typeName);
                writer.WriteASCII(">");
                writer.WriteASCII(id);
                writer.WriteASCII(">");
                writer.WriteASCII(localisedField.Name);
                writer.WriteASCII("\"\r\n");

                // Msgid is the actual value:
                writer.WriteASCII("msgid \"");

                // Any double quotes are also escaped.
                var rawFieldValue = localisedField.FieldInfo.GetValue(result);

                // Do we want to simplify the canvas rendering for translation
                rawFieldValue = await Events.Locale.PotFieldValue.Dispatch(context, rawFieldValue, localisedField, translationServiceConfig);

                if (rawFieldValue is string)
                {
                    writer.WriteS(EscapeForPo((string)rawFieldValue));
                }
                else if (rawFieldValue != null)
                {
                    // E.g. prices
                    writer.WriteS(rawFieldValue.ToString());
                }

                // The final quote to close msgid:
                writer.WriteASCII("\"\r\n");

                // Next, either append a blank msgstr or a value depending on if we've got a locale specific object.
                // (PO files have a locale specific value here, POT does not).
                writer.WriteASCII("msgstr \"");
                writer.WriteASCII("\"\r\n\r\n");
            }

            // Output to body:
            await writer.CopyToAsync(Response.Body);
            writer.Reset(null);
        }

        writer.Release();
    }

    /// <summary>
    /// Escapes a value for use in PO/POT files.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private string EscapeForPo(string value)
    {
        if (value == null)
        {
            return "";
        }

        // Newlines are escaped by making them literal as well as surrounding the line in quotes.
        // Hello
        // world

        // becomes
        // "Hello\n"
        // "world"

        return value.Replace("\"", "\\\"").Replace("\r\n", "\n").Replace("\n", "\\n\"\n\"");
    }

}