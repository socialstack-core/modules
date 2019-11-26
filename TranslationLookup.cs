using System;
using System.Collections;
using System.Collections.Generic;


namespace Api.Translate
{
    /// <summary>
    /// A mapping of key to group of translations.
    /// For example, "en_GB" maps to a set of all British English translations.
    /// It's generic so you can conveniently reuse this for mapping locales, content types, content etc.
    /// Intended to be readonly - edit Translations objects and generate lookups from those.
    /// </summary>
    public class TranslationLookup<T> : IEnumerable<KeyValuePair<T, Translations>>
    {

        /// <summary>
        /// Creates a lookup from the given set of translations and a function which selects a particular field from them.
        /// </summary>
        /// <param name="translations"></param>
        /// <param name="mapper"></param>
        /// <returns></returns>
        public static TranslationLookup<T> Create(List<Translation> translations, Func<Translation, T> mapper)
        {
            var result = new TranslationLookup<T>();

            foreach (var translation in translations)
            {
                result.GetOrCreate(mapper(translation)).Add(translation);
            }

            return result;
        }

        /// <summary>
        /// The underlying lookup.
        /// </summary>
        private Dictionary<T, Translations> _lookup = new Dictionary<T, Translations>();

        /// <summary>
        /// Gets or creates a translation set for the given key.
        /// The key is e.g. the locale that the translations are all for.
        /// </summary>
        /// <param name="key">E.g. "en_GB" or "products", depending on how you're grouping up your translations.</param>
        /// <returns></returns>
        public Translations GetOrCreate(T key)
        {
            var result = this[key];
            if (result == null)
            {
                result = new Translations();
                _lookup[key] = result;
            }
            return result;
        }

		/// <summary>
		/// Loop through all translations in this lookup.
		/// </summary>
		/// <returns></returns>
        public IEnumerator<KeyValuePair<T, Translations>> GetEnumerator()
        {
            foreach (var kvp in _lookup)
            {
                yield return kvp;
            }
        }

		/// <summary>
		/// Loop through all translations in this lookup.
		/// </summary>
		/// <returns></returns>
		IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var kvp in _lookup)
            {
                yield return kvp;
            }
        }

        /// <summary>
        /// Gets a translation set by the key which is e.g. the locale that the translations are for.
        /// </summary>
        /// <param name="key">E.g. "en_GB" or "products", depending on how you're grouping up your translations.</param>
        /// <returns></returns>
        public Translations this[T key]
        {
            get
            {
                Translations value;
                _lookup.TryGetValue(key, out value);
                return value;
            }
        }
    }

}
