using System;
using Forma.Product;
using Forma.Storage;
using UnityEngine;
namespace Forma.Runtime
{
    public sealed class UnityProfileCodec : IProfileCodec
    {
        [Serializable] struct Header { public int schemaVersion; }
        public string Encode(ProductProfile profile) => JsonUtility.ToJson(profile,true);
        public ProductProfile Decode(string json)
        {
            var header=JsonUtility.FromJson<Header>(json);
            if(header.schemaVersion!=2)return new ProductProfile {schemaVersion=header.schemaVersion};
            return JsonUtility.FromJson<ProductProfile>(json);
        }
    }
}
