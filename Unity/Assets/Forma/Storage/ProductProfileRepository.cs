using System;
using System.IO;
using System.Text;
using Forma.Product;
namespace Forma.Storage
{
    public interface IProfileCodec
    { string Encode(ProductProfile profile); ProductProfile Decode(string json); }
    public sealed class ProductProfileRepository : IProductProfileRepository
    {
        readonly string root,path;
        readonly IProfileCodec codec;
        public ProductProfileRepository(string root,IProfileCodec codec)
        {
            if(string.IsNullOrWhiteSpace(root))throw new ArgumentException("Storage root required.");
            this.root=root;path=Path.Combine(root,"product-profile.json");this.codec=codec ?? throw new ArgumentNullException(nameof(codec));
        }
        public ProfileLoadResult Load()
        {
            try
            {
                if(!File.Exists(path))return new ProfileLoadResult(ProfileLoadStatus.Missing);
                var p=codec.Decode(File.ReadAllText(path));
                if(p==null)return new ProfileLoadResult(ProfileLoadStatus.Invalid);
                if(p.schemaVersion!=2)return new ProfileLoadResult(ProfileLoadStatus.Unsupported);
                return ProductProfile.IsValid(p)?new ProfileLoadResult(ProfileLoadStatus.Loaded,p):new ProfileLoadResult(ProfileLoadStatus.Invalid);
            }
            catch(ArgumentException){return new ProfileLoadResult(ProfileLoadStatus.Invalid);}
            catch(IOException){return new ProfileLoadResult(ProfileLoadStatus.Unavailable);}
            catch(UnauthorizedAccessException){return new ProfileLoadResult(ProfileLoadStatus.Unavailable);}
        }
        public void Save(ProductProfile p)
        {
            if(!ProductProfile.IsValid(p))throw new ArgumentException("Invalid profile.");
            var existing=Load();
            if(existing.Status!=ProfileLoadStatus.Missing && existing.Status!=ProfileLoadStatus.Loaded)
                throw new InvalidOperationException("Existing profile cannot be overwritten. Explicit reset or retry is required.");
            Directory.CreateDirectory(root);
            string temporary=path+".tmp";
            byte[] bytes=Encoding.UTF8.GetBytes(codec.Encode(p.Copy()));
            using(var file=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None))
            { file.Write(bytes,0,bytes.Length);file.Flush(true); }
            if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);
        }
        public void Delete()
        { if(File.Exists(path))File.Delete(path);if(File.Exists(path+".tmp"))File.Delete(path+".tmp"); }
        public string Export()
        {
            var result=Load();
            if(result.Status!=ProfileLoadStatus.Loaded)throw new InvalidOperationException("No readable product profile to export.");
            return codec.Encode(result.Profile);
        }
    }
}
