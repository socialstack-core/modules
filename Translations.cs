using System.Collections;
using System.Collections.Generic;
using System.Text;
using Api.Database;


namespace Api.Translate
{
    /// <summary>
    /// A group of translations. Can be organised in a variety of ways, e.g. by locale or content group etc.
    /// </summary>
    public class Translations : IEnumerable<Translation>
    {
        /// <summary>
        /// The key of this group of translations within a parent group of translations.
        /// </summary>
        public string Key;
        /// <summary>
        /// The complete set of all translations within this group.
        /// </summary>
        private List<Translation> _allTranslations;
        /// <summary>
        /// Used when the locale is explicitly known, but the set may be empty.
        /// </summary>
        private int _locale;

        /// <summary>
        /// Gets the locale of the first translation in this set.
        /// Only rely on this when you know they're all the same locale or know it was explicitly set (it could easily be mixed).
        /// </summary>
        public int Locale
        {
            get {
                if (_locale != 0)
                {
                    return _locale;
                }

                if (_allTranslations.Count == 0)
                {
                    return 0;
                }

                return _allTranslations[0].Locale;
            }
        }

		/// <summary>
		/// Create a new set of translations using the given list.
		/// </summary>
		/// <param name="translations"></param>
		/// <param name="locale"></param>
        public Translations(List<Translation> translations, int locale)
        {
            _locale = locale;
            _allTranslations = translations;
        }

		/// <summary>
		/// Create an empty translations set.
		/// </summary>
        public Translations()
        {
            _allTranslations = new List<Translation>();
        }

        /// <summary>
        /// Group up translations within this set by locale - e.g. "en_GB".
        /// </summary>
        /// <returns></returns>
        public TranslationLookup<int> GroupByLocale()
        {
            return TranslationLookup<int>.Create(_allTranslations, (Translation translation) => translation.Locale);
        }

        /// <summary>
        /// Group up translations within this set by content type - e.g. "products".
        /// </summary>
        /// <returns></returns>
        public TranslationLookup<int> GroupByContentType()
        {
            return TranslationLookup<int>.Create(_allTranslations, (Translation translation) => translation.ContentTypeId);
        }

        /// <summary>
        /// Group up translations within this set by their content ID (e.g. by forum ID).
        /// </summary>
        /// <returns></returns>
        public TranslationLookup<int> GroupByContentId()
        {
            return TranslationLookup<int>.Create(_allTranslations, (Translation translation) => translation.ContentId);
        }

		/// <summary>
		/// Gets this translation set grouped by content within a particular group.
		/// For example, GetContent(typeof(Gallery)) could give a lookup organised by gallery ID.
		/// </summary>
		/// <param name="contentType"></param>
		/// <returns></returns>
		public TranslationLookup<int> GetContent(System.Type contentType)
		{
			return GetGroup(contentType).GroupByContentId();
		}

		/// <summary>
		/// Gets this translation set grouped by content within a particular group.
		/// For example, GetContent(typeof(Gallery)) could give a lookup organised by gallery ID.
		/// </summary>
		/// <param name="contentTypeId"></param>
		/// <returns></returns>
		public TranslationLookup<int> GetContent(int contentTypeId)
		{
			return GetGroup(contentTypeId).GroupByContentId();
		}

		/// <summary>
		/// Spots the difference between 'this' set and a newer given one.
		/// Note that 'this' set must have translation row IDs (i.e. originate from the database).
		/// The newerSet doesn't require them and will compare by msg ID instead.
		/// </summary>
		/// <param name="newerSet"></param>
		public DiffSet<Translation> Diff(Translations newerSet)
        {
            // Find all the entries that have been added, changed or deleted.
            var thisLookup = ToHierarchyKeyDictionary();
            var newerLookup = newerSet.ToHierarchyKeyDictionary();

            // 1. Deleted entries are ones that exist here but not in the newer set.
            //    Updated entries are in both but have different text.

            var resultDiff = new DiffSet<Translation>();

            foreach (var kvp in thisLookup)
            {
                var currentValue = kvp.Value;

                Translation newerValue;
                if (newerLookup.TryGetValue(kvp.Key, out newerValue))
                {
                    // It's in the newer lookup too - has it changed?
                    if (newerValue.Html != currentValue.Html)
                    {
                        // It changed - retain the ID for fast updates but use the new value:
                        newerValue.Id = currentValue.Id;
                        resultDiff.Changed.Add(newerValue);
                    }
                }
                else
                {
                    // It got removed:
                    resultDiff.Removed.Add(kvp.Value);
                }
                
            }

            // 2. Added entries are ones that exist in the newer set but not here.
            foreach (var kvp in newerLookup)
            {

                Translation currentValue;
                if (!thisLookup.TryGetValue(kvp.Key, out currentValue))
                {
                    // It's been added:
                    resultDiff.Added.Add(kvp.Value);
                }
            }

            return resultDiff;
        }

		/// <summary>
		/// Gets all translations for a particular content type.
		/// </summary>
		/// <param name="contentType">E.g. typeof(Gallery) to get all gallery translations.</param>
		/// <returns></returns>
		public Translations GetGroup(System.Type contentType)
		{
			return GetGroup(ContentTypes.GetId(contentType));
		}

		/// <summary>
		/// Gets all translations for a particular content type.
		/// </summary>
		/// <param name="contentTypeId"></param>
		/// <returns></returns>
		public Translations GetGroup(int contentTypeId)
		{
			var result = new Translations();
			for (var i = 0; i < _allTranslations.Count; i++)
			{
				if (_allTranslations[i].ContentTypeId == contentTypeId)
				{
					result.Add(_allTranslations[i]);
				}
			}
			return result;
		}

		/// <summary>
		/// Get first translation by the given key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public Translation GetFirstByKey(string key)
		{
			for (var i = 0; i < _allTranslations.Count; i++)
			{
				if (_allTranslations[i].Key == key)
				{
					return _allTranslations[i];
				}
			}

			return null;
		}

		/// <summary>
		/// Gets a HTML translation from this translation set for the given key.
		/// For example this set of translations could be for a particular product and key is e.g. "name".
		/// </summary>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public string GetValue(string key, string defaultValue)
		{
			// Get first trans for the key 
			// (Note there would only be a few translations in the set that uses this so it's never worth building a lookup).
			var translation = GetFirstByKey(key);

			if (translation == null || string.IsNullOrEmpty(translation.Html))
			{
				return defaultValue;
			}

			return translation.Html;
		}

		/// <summary>
		/// Group up translations within this set by the hierarchy key - that's Locale/ContentTypeId/ContentId/Key
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, Translation> ToHierarchyKeyDictionary()
        {
            var result = new Dictionary<string, Translation>();

            foreach (var translation in _allTranslations)
            {
                result[translation.HierarchyKey] = translation;
            }

            return result;
        }

        /// <summary>
        /// Group up translations within this set by key - e.g. product title, product body etc.
        /// </summary>
        /// <returns></returns>
        public TranslationLookup<string> GroupByKey()
        {
            return TranslationLookup<string>.Create(_allTranslations, (Translation translation) => translation.Key);
        }

        /// <summary>
        /// Removes a translation from this set.
        /// </summary>
        /// <param name="translation"></param>
        public void Remove(Translation translation)
        {
            _allTranslations.Remove(translation);
        }

        /// <summary>
        /// Adds a translation to this set.
        /// </summary>
        /// <param name="translation">The translation to add.</param>
        public void Add(Translation translation)
        {
            _allTranslations.Add(translation);
        }

        /// <summary>
        /// Creates a PO file using the given set of translations to lookup through.
        /// </summary>
        /// <param name="lookupVia"></param>
        /// <returns></returns>
        public string ToPo(Translations lookupVia)
        {
            StringBuilder builder = new StringBuilder();
            ToPo(lookupVia, builder);
            return builder.ToString();
        }

		/// <summary>
		/// Creates a PO file using the given set of translations to lookup through.
		/// </summary>
		/// <param name="lookupVia"></param>
		/// <param name="builder"></param>
		/// <returns></returns>
		public void ToPo(Translations lookupVia, StringBuilder builder)
        {
            // Next, for each translation in the US set, find the matching translation in the target set.
            // "Matching" means a match on ContentId, ContentTypeId and Key.

            foreach (var translation in _allTranslations)
            {
                // Find a "matching" translation:
                var match = lookupVia.FindOtherLocale(translation);
                
                // Write out the translation as a PO:
                translation.ToPo(match, builder);

            }

        }

		/// <summary>
		/// This translation set as a JSON string. Note that it is just a direct list of translations, optionally with content filtered out.
		/// If you want more filtering, do so at the Translations object level.
		/// </summary>
		/// <param name="omitContent">Optionally don't include content - that's e.g. translations for individual forums - in the outputted JSON.</param>
		public string AsJson(bool omitContent = true)
        {
            var toSerialize = _allTranslations;
            if (omitContent)
            {
                // Filter out all translations not on any content:
                toSerialize = toSerialize.FindAll(translation => translation.ContentId == 0);
            }

            return Newtonsoft.Json.JsonConvert.SerializeObject(toSerialize);
        }

		/// <summary>
		/// Saves this translation set to a file at the given path.
		/// If the directory doesn't exist, this will attempt to create it.
		/// </summary>
		/// <param name="targetPath"></param>
		/// <param name="omitContent">Optionally don't include content - that's e.g. translations for individual products - in the outputted JSON.</param>
		public void SaveJson(string targetPath, bool omitContent = true)
        {
            var dirName = System.IO.Path.GetDirectoryName(targetPath);
            if (!System.IO.Directory.Exists(dirName))
            {
                System.IO.Directory.CreateDirectory(dirName);
            }

            // Get the JSON:
            var jsonString = AsJson(omitContent);

            // Output to file now:
            System.IO.File.WriteAllText(targetPath, jsonString);
        }

        /// <summary>
        /// Applies a diff set to this list of translations.
        /// Note that this will overwrite the contents of this set.
        /// </summary>
        /// <param name="patch">The diff to apply.</param>
        public void Apply(DiffSet<Translation> patch)
        {
            if (patch.Removed != null || patch.Changed != null)
            {
                // We'll use an ID lookup for performance here.
                Dictionary<int, Translation> lookup = new Dictionary<int, Translation>();

                foreach (var translation in _allTranslations)
                {
                    lookup[translation.Id] = translation;
                }

                if (patch.Removed != null)
                {
                    for (var i = 0; i < patch.Removed.Count; i++)
                    {
                        // Remove it from the lookup:
                        lookup.Remove(patch.Removed[i].Id);
                    }
                }

                if (patch.Changed != null)
                {
                    // Replace translations with the same ID with ones in the patch.
                    for (var i = 0; i < patch.Changed.Count; i++)
                    {
                        // Straight swap:
                        var translation = patch.Changed[i];
                        lookup[translation.Id] = translation;
                    }
                }

                // Convert the lookup back to the reconstructed translation list:
                _allTranslations = new List<Translation>();

                foreach (var kvp in lookup)
                {
                    _allTranslations.Add(kvp.Value);
                }
            }

            if (patch.Added != null)
            {
                // Just append the whole set:
                _allTranslations.AddRange(patch.Added);
            }
        }

        /// <summary>
        /// Writes these translations to the POT file format - a common interchange format for translations.
        /// </summary>
        public string ToPot()
        {
            StringBuilder builder = new StringBuilder();
            ToPot(builder);
            return builder.ToString();
        }

        /// <summary>
        /// Writes these translations to the POT file format - a common interchange format for translations.
        /// </summary>
        public void ToPot(StringBuilder builder)
        {
            foreach (var translation in _allTranslations)
            {
                translation.ToPot(builder);
            }
        }

		/// <summary>
		/// Finds a translation in this set like the given one, ignoring locale, Html and Id. All other fields must match.
		/// </summary>
		/// <param name="toMatch">The translation to search for.</param>
		/// <returns>Null if there was no match.</returns>
		public Translation FindOtherLocale(Translation toMatch)
        {
            if (toMatch == null)
            {
                return null;
            }

            foreach (var translation in _allTranslations)
            {
                // content type, content and key match check:
                if (translation.ContentTypeId == toMatch.ContentTypeId && 
                    translation.ContentId == toMatch.ContentId && 
                    translation.Key == toMatch.Key)
                {
                    // Got it!
                    return translation;
                }
            }

            return null;
        }

        /// <summary>
        /// All the translations in a set.
        /// </summary>
        public List<Translation> All
        {
            get
            {
                return _allTranslations;
            }
        }

        /// <summary>
        /// Loop through all translations within this group.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Translation> GetEnumerator()
        {
            foreach (var translation in _allTranslations) {
                yield return translation;
            }
        }

        /// <summary>
        /// Loop through all translations within this group.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var translation in _allTranslations)
            {
                yield return translation;
            }
        }
    }
    
}
