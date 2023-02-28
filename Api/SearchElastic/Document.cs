using Nest;
using System;
using System.Collections.Generic;

namespace Api.SearchElastic
{
    /// <summary>
    /// The definition of an indedxed document within elastic
    /// </summary>
    /// 

    public class Document
    {
        [Text]
        public string Id { get; set; }
        [Text]
        public string Hash { get; set; }
        [Text]
        public string Title { get; set; }
        [Text]
        public string Content { get; set; }
        [Text]
        public string Keywords { get; set; }
        [Text]
        public IEnumerable<string> Headings { get; set; }
        [Text]
        public string Url { get; set; }
        [Text]
        public string CheckSum { get; set; }

        // keyword fields to allow for filtering 
        [Keyword]
        public string ContentType { get; set; }
        [Keyword]
        public IEnumerable<string> Tags { get; set; }
        [Keyword]
        public DateTime TimeStamp { get; set; }

        // non indexed field used when rendering
        [Text(Ignore = true)]
        public string Highlights { get; set; }
        [Text(Ignore = true)]
        public double? Score { get; set; }

        // dynamic object to hold primary data
        [Object]
        public dynamic PrimaryObject { get; set; }

    }
}
