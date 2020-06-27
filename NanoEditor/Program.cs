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

    public class ManualAttribute : Attribute
    {
        public string Data { get; private set; }

        public ManualAttribute(string data) => Data = data;
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
   
    [ManualAttribute(@"Usage: pwd
Print name of current/working directory")]
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

    [ManualAttribute(@"Usage: ls (DIRECTORY)
List directory contents

List information about the DIRECTORY (the current directory by default).")]
    public class Ls : Command
    {
        private string _fileName;
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
                if(!_directory.ChildrenTreeNodes.ContainsKey(_fileName)) return;
                Dictionary<string, TreeNode> nodes = (_directory.ChildrenTreeNodes[_fileName] as Directory)?.ChildrenTreeNodes;
                if(nodes != null) ShowChildren(nodes);
            }
        }

        private void ShowChildren(Dictionary<string, TreeNode> nodes)
        {
            foreach (TreeNode node in nodes.Values)
                Console.WriteLine(node.Name);
        }
    }

    [ManualAttribute(@"Usage: cd [DIR]
Change the current directory to DIR.")]
    public class Cd : Command
    {
        private string _fileName;
        public Cd(Environment environment, string[] parameters) : base(environment, parameters)
        {
            if(parameters.Length > 0)
                _fileName = _parameters?[0];
        }

        public override void Perform()
        {
            if (_fileName != "..")
            {
                if (_directory.ChildrenTreeNodes.ContainsKey(_fileName))
                {
                    if((_directory.ChildrenTreeNodes[_fileName].IsDirectory))
                        _environment.Current = _directory.ChildrenTreeNodes[_fileName] as Directory;
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
    
    [ManualAttribute(@"Usage: mkdir [DIRECTORY]
mkdir - make directories

Create the DIRECTORY, if they do not already exist. ")]
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
    
    [ManualAttribute(@"Usage: touch [FILE]
mkdir - change file timestamps or create one more.

Create the FILE, if they do not already exist. ")]
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

    [ManualAttribute(@"Usage: clear
clear - clear the terminal screen.

clear clears your screen if this is possible. clear ignores any command-line parameters that may be present. ")]
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
    
    [ManualAttribute(@"Usage: cat [FILE]
cat - print on the standard output.

Concatenate FILE to standard output.")]
    public class Cat : Command
    {
        private string _fileName;

        public Cat(Environment environment, string[] parameters) : base(environment, parameters)
        {
            _fileName = _parameters[0];
        }

        public override void Perform()
        {
            if(_directory.ChildrenTreeNodes.ContainsKey(_fileName))
            {
                TreeNode node = _directory.ChildrenTreeNodes[_fileName];
                if (!node.IsDirectory)
                {
                    File file = node as File;
                    if(file.Readable) Console.WriteLine(file.Content);
                    else Console.WriteLine("Error: you have not permissions to read this file");
                }
            }
                    
        }
    }

    [ManualAttribute(@"Usage: write [FILE] [WORDS..]
write - override contents of the file.

Concatenates all of the WORDS and overrides with them full contents of the FILE")]
    public class Write : Command
    {
        private string _fileName;

        public Write(Environment environment, string[] parameters) : base(environment, parameters)
        {
            _fileName = _parameters[0];
        }

        public override void Perform()
        {
            if(_directory.ChildrenTreeNodes.ContainsKey(_fileName))
            {
                TreeNode node = _directory.ChildrenTreeNodes[_fileName];
                if (!node.IsDirectory)
                {
                    File file = node as File;
                    if(file.Writeable) file.Content = string.Join(' ',_parameters.Skip(1));
                    else Console.WriteLine("Error: you have not permissions to write to this file");
                }
            }
                    
        }
    }
    
    [ManualAttribute(@"Usage: rm [ENTITY]
rm - remove files or directories.

rm removes each specified ENTITY.")]
    public class Rm : Command
    {
        private string _fileName;

        public Rm(Environment environment, string[] parameters) : base(environment, parameters)
        {
            _fileName = parameters[0];
        }

        public override void Perform()
        {
            if (_directory.ChildrenTreeNodes.ContainsKey(_fileName))
            {
                _directory.ChildrenTreeNodes.Remove(_fileName);
            }
        }
    }
    
    [ManualAttribute(@"Usage: mv [SOURCE] [DEST]
mv - move (rename) files.

Rename SOURCE to DEST.")]
    public class Mv : Command
    {
        private string _fileName;
        private string _newFileName;

        public Mv(Environment environment, string[] parameters) : base(environment, parameters)
        {
            _fileName = parameters[0];
            _newFileName = parameters[1];
        }

        public override void Perform()
        {
            if (_directory.ChildrenTreeNodes.ContainsKey(_fileName))
            {
                TreeNode node = _directory.ChildrenTreeNodes[_fileName];
                node.Name = _newFileName;
                _directory.ChildrenTreeNodes.Remove(_fileName);
                _directory.ChildrenTreeNodes[_newFileName] = node;
            }
        }
    }
    
    [ManualAttribute(@"Usage: chmod [FILE] [MODE]
chmod - change file access permissions.

chmod changes the permissions of given file according to mode, which can be an decimal number representing the bit pattern for the new permissions.")]
    public class Chmod : Command
    {
        private string _fileName;
        private string _mod;

        public Chmod(Environment environment, string[] parameters) : base(environment, parameters)
        {
            _fileName = parameters[0];
            _mod = parameters[1];
        }

        public override void Perform()
        {
            if (_directory.ChildrenTreeNodes.ContainsKey(_fileName))
            {
                TreeNode node = _directory.ChildrenTreeNodes[_fileName];
                if (int.TryParse(_mod, out int mod))
                {
                    if (!node.IsDirectory)
                    {
                        (node as File).ChangeMod((Mods) mod);
                    }
                }
                else Console.WriteLine("Unable to set mod");
            }
        }
    }
    
    [ManualAttribute(@"Usage: help
help - Display helpful information about builtin commands.")]
    public class Help : Command
    {
        public Help(Environment environment, string[] parameters) : base(environment, parameters) { }

        public override void Perform()
        {
            Console.WriteLine(@"pwd - get current location
ls - listen directory to get files inside
cd - change current directory
mkdir - create new directory
touch - create new file
chmod - change access level to file
cat - read file contents
write - override file contents
rm - remove file or directory
mv - rename file or directory
clear - clear console
exit - leave this environment
man [command] - find out more about [command]
help - show this screen again");
        }
    }
    
    [ManualAttribute(@"Usage: man [NAME]
man - format and display the on-line manual pages.

NAME is normally the name of the manual page, which is typically the name of a command, function, or file")]
    public class Man : Command
    {
        private string _command;

        public Man(Environment environment, string[] parameters) : base(environment, parameters)
        {
            _command = parameters[0];
        }

        private string GetManual(Type type)
        {
            return type.CustomAttributes.First(x => x.AttributeType == typeof(ManualAttribute)).ConstructorArguments[0]
                .Value.ToString();
        }
        
        public override void Perform()
        {
            string result = _command switch
            {
                "pwd" => GetManual(typeof(Pwd)),
                "ls" => GetManual(typeof(Ls)),
                "cd" => GetManual(typeof(Cd)),
                "mkdir" => GetManual(typeof(Mkdir)),
                "touch" => GetManual(typeof(Touch)),
                "clear" => GetManual(typeof(Clear)),
                "cat" => GetManual(typeof(Cat)),
                "write" => GetManual(typeof(Write)),
                "rm" => GetManual(typeof(Rm)),
                "mv" => GetManual(typeof(Mv)),
                "chmod" => GetManual(typeof(Chmod)),
                "help" => GetManual(typeof(Help)),
                "man" => GetManual(typeof(Man)),
                _ => $"There is no manual specially for {_command}"
            };
            Console.WriteLine(result);
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
                {"cat", (parameters) => new Cat(this, parameters)},
                {"write", (parameters) => new Write(this, parameters)},
                {"rm", (parameters) => new Rm(this, parameters)},
                {"mv", (parameters) => new Mv(this, parameters)},
                {"chmod", (parameters) => new Chmod(this, parameters)},
                {"help", (parameters) => new Help(this, null)},
                {"man", (parameters) => new Man(this, parameters)},
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
                if(_actions.ContainsKey(command[0]))
                    _actions[command[0]](command.Skip(1).ToArray()).Perform();
                else
                    Console.WriteLine("Unknown command! Type `help` to find out more about commands and get full theirs list.");
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
        No = 0,
        Readonly,
        Writeable,
        ReadWrite
    }
}