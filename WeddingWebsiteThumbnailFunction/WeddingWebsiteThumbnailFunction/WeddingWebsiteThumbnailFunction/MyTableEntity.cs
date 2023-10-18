using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace WeddingWebsiteThumbnailFunction {
    public class MyTableEntity : TableEntity {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string fullSizeUrl { get; set; }
        public string thumbnailUrl { get; set; }
        public string name { get; set; }

        public MyTableEntity() { }
    }
}