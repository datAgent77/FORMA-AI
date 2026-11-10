public static class LocalDataLocation
{
    // Existing deployments can keep their explicit location. Default is durable and git-ignored, outside wwwroot.
    public static string Resolve(string contentRoot,out string legacyRoot)
    {
        var configured=Environment.GetEnvironmentVariable("FORMA_SIM_DATA");legacyRoot=null;
        if(!string.IsNullOrWhiteSpace(configured))return Path.GetFullPath(configured);
        string root=Path.Combine(contentRoot,".local-data");
        legacyRoot=Path.Combine(Path.GetTempPath(),"forma-web-simulation");
        Directory.CreateDirectory(root);string marker=Path.Combine(root,".legacy-import-complete");
        if(!File.Exists(marker))
        {
            if(Directory.Exists(legacyRoot))foreach(string directory in Directory.EnumerateDirectories(legacyRoot))
            {
                string id=Path.GetFileName(directory);if(!Guid.TryParseExact(id,"N",out _))continue;
                string destination=Path.Combine(root,id);Directory.CreateDirectory(destination);
                foreach(string name in new[]{"product-profile.json","simulation-history.json","athlete-profile.json","personal-training.json"})
                {
                    string source=Path.Combine(directory,name),target=Path.Combine(destination,name);
                    if(File.Exists(source) && !File.Exists(target))File.Copy(source,target,false);
                }
            }
            File.WriteAllText(marker,"Legacy import attempted once; never resurrect deleted profiles.");
        }
        return root;
    }
}
