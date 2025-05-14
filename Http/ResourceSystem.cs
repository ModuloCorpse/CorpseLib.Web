namespace CorpseLib.Web.Http
{
    public class ResourceSystem
    {
        public abstract class Resource
        {
            public virtual List<KeyValuePair<Path, Resource>> Flatten(Path root) => [ new(root, this) ];
        }

        public class Directory : Resource
        {
            private readonly Dictionary<string, Resource> m_Resources = [];

            public Resource? Get(Path path)
            {
                string currentPath = path.CurrentPath;
                Path? nextPath = path.NextPath();

                if (nextPath == null)
                {
                    if (m_Resources.TryGetValue(currentPath, out Resource? resource))
                        return resource;
                    else
                        return null;
                }
                else if (m_Resources.TryGetValue(currentPath, out Resource? resource) && resource is Directory directory)
                    return directory.Get(nextPath);
                else
                    return null;
            }

            public void Add(Path path, Resource resourceToAdd)
            {
                string currentPath = path.CurrentPath;
                Path? nextPath = path.NextPath();
                if (nextPath == null)
                    m_Resources[currentPath] = resourceToAdd;
                else
                {
                    if (!m_Resources.TryGetValue(currentPath, out Resource? resource))
                    {
                        Directory directory = new();
                        m_Resources[currentPath] = directory;
                        directory.Add(nextPath, resourceToAdd);
                    }
                    else if (resource is Directory directory)
                        directory.Add(nextPath, resourceToAdd);
                }
            }
            public override List<KeyValuePair<Path, Resource>> Flatten(Path root)
            {
                List<KeyValuePair<Path, Resource>> list = [];
                foreach (var pair in m_Resources)
                    list.AddRange(pair.Value.Flatten(root.Append(pair.Key)));
                return list;
            }
        }

        private readonly Directory m_RootDirectory = new(); // Represent /

        public Resource? Get(Path path)
        {
            if (path.IsEmpty())
                return m_RootDirectory;
            return m_RootDirectory.Get(path);
        }

        public void Add(Path path, Resource resource)
        {
            if (path.IsEmpty())
                return;
            m_RootDirectory.Add(path, resource);
        }

        public List<KeyValuePair<Path, Resource>> Flatten() => Flatten(new());
        public List<KeyValuePair<Path, Resource>> Flatten(Path root) => m_RootDirectory.Flatten(root);
    }
}
