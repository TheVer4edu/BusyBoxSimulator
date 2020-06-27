using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NanoEditor
{
    class Program
    {
        static void Main(string[] args)
        {
            Environment env = new Environment();
            env.Launch();
        }
    }

    public abstract class Command
    {
        protected Directory _directory;
        protected Environment _environment;
        protected string[] _parameters;
        
        public Command(Environment environment, string[] parameters)
        {
            _environment = environment;
            _directory = environment.Current;
            _parameters = parameters;

        }
        
        public abstract void Perform();
    }

    public class Pwd : Command
    {
        public Pwd(Environment environment, string[] parameters) : base(environment, parameters) {}
        
        public override void Perform()
        {
            StringBuilder result = CreateFullPath(ref _directory, new StringBuilder());
            Console.WriteLine(result.ToString());
        }

        private StringBuilder CreateFullPath(ref Directory current, StringBuilder sb)
        {
            if (current.ParentElement == null) return sb.Insert(0, "/");
            sb.Insert(0, current.Name + "/");
            current = current.ParentElement as Directory;
            return CreateFullPath(ref current, sb);
        }
    }

    public class Ls : Command
    {
        private string _fileName = null;
        public Ls(Environment environment, string[] parameters) : base(environment, parameters)
        {
            if(parameters.Length > 0)
                _fileName = _parameters?[0];
        }

        public override void Perform()
        {
            if (_fileName == null)
                ShowChildren(_directory.ChildrenTreeNodes);
            else
            {
                Dictionary<string, TreeNode> nodes = (_directory.ChildrenTreeNodes.FirstOrDefault(x => x.Value.Name == _fileName).Value as Directory)?.ChildrenTreeNodes;
                if(nodes != null) ShowChildren(nodes);
            }
        }

        private void ShowChildren(Dictionary<string, TreeNode> nodes)
        {
            foreach (TreeNode node in nodes.Values)
                Console.WriteLine(node.Name);
        }
    }

    public class Cd : Command
    {
        private string _fileName = null;
        public Cd(Environment environment, string[] parameters) : base(environment, parameters)
        {
            if(parameters.Length > 0)
                _fileName = _parameters?[0];
        }

        public override void Perform()
        {
            if (_fileName != "..")
            {
                if (_directory.ChildrenTreeNodes.Select(x => x.Value.Name).Contains(_fileName))
                {
                    if((_directory.ChildrenTreeNodes.First(x => x.Value.Name == _fileName).Value.IsDirectory))
                        _environment.Current = _directory.ChildrenTreeNodes.First(x => x.Value.Name == _fileName).Value as Directory;
                    else
                        Console.WriteLine($"Unable cd to file \"{_fileName}\"");
                }
            }
            else
            {
                if(_environment.Current.ParentElement == null) return;
                _environment.Current = _environment.Current.ParentElement as Directory;
            }
        }
    }
    
    public class Mkdir : Command
    {
        private string _fileName;

        public Mkdir(Environment environment, string[] parameters) : base(environment, parameters)
        {
            _fileName = _parameters[0];
        }

        public override void Perform()
        {
            _directory.ChildrenTreeNodes[_fileName] = new Directory(_fileName, _directory);
        }
    }
    
    public class Touch : Command
    
    {
        private string _fileName;

        public Touch(Environment environment, string[] parameters) : base(environment, parameters)
        {
            _fileName = _parameters[0];
        }

        public override void Perform()
        {
            _directory.ChildrenTreeNodes[_fileName] = new File(_fileName);
        }
    }

    public class Clear : Command
    {
        public Clear(Environment environment, string[] parameters) : base(environment, parameters)
        {
        }

        public override void Perform()
        {
            Console.Clear();
        }
    }
    
    
    /**
     *  cat [filename] - прочитать содержимое файла
        rm [name] - удалить директорию или файл из системы
        write [name] [content] - записать [content] в файл [name]
        mv [oldName] [newName] - изменить [oldName] на [newName] (переименование)
        help - вывести вот это всё
     */
    public class Cat : Command
    {
        public Cat(Environment environment, string[] parameters) : base(environment, parameters)
        {
            
        }

        public override void Perform()
        {
            throw new NotImplementedException();
        }
    }


    public class Environment
    {
        private Dictionary<string, Func<string[], Command>> _actions;
        public Directory Current { get; set; } = new Directory("/", null);
        
        public Environment()
        {
            _actions = new Dictionary<string, Func<string[], Command>>()
            {
                {"pwd", (parameters) => new Pwd(this, null)},
                {"ls", (parameters) => new Ls(this, parameters)},
                {"cd", (parameters) => new Cd(this, parameters)},
                {"mkdir", (parameters) => new Mkdir(this, parameters)},
                {"touch", (parameters) => new Touch(this, parameters)},
                {"clear", (parameters) => new Clear(this, null)},
            };
        }

        public void Launch()
        {
            string line = null;
            Console.Write("$: ");
            line = Console.ReadLine();
            while (line != "exit")
            {
                string[] command;
                if (line.Contains(' '))
                    command = line.Split(' ');
                else 
                    command = new []{line};
                _actions[command[0]](command.Skip(1).ToArray()).Perform();
                Console.Write("$: ");
                line = Console.ReadLine();
            }
        }
    }

    public abstract class TreeNode
    {
        public bool IsDirectory { get; protected set; }
        public string Name { get; set; }

        public TreeNode(string name)
        {
            this.Name = name;
        }
    }

    public class Directory : TreeNode
    {
        public Dictionary<string, TreeNode> ChildrenTreeNodes { get; } = new Dictionary<string, TreeNode>();
        public TreeNode ParentElement { get; set; }

        public Directory(string name, Directory parent) : base(name)
        {
            ParentElement = parent;
            IsDirectory = true;
        }
    }

    class File : TreeNode
    {
        public string Content { get; set; }
        public bool Readable { get; private set; } = true;
        public bool Writeable { get; private set; } = true;

        public void ChangeMod(Mods mod)
        {
            switch (mod) 
            {
                case Mods.No:
                    Readable = false;
                    Writeable = false;
                    break;
                case Mods.Readonly:
                    Readable = true;
                    Writeable = false;
                    break;
                case Mods.Writeable:
                    Readable = false;
                    Writeable = true;
                    break;
                case Mods.ReadWrite:
                    Readable = true;
                    Writeable = false;
                    break;
            }
        }
        
        public File(string name) : base(name) {}
    }

    enum Mods
    {
        No,
        Readonly,
        Writeable,
        ReadWrite
    }
}