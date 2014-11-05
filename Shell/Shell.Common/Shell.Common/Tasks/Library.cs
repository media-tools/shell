using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;

namespace Shell.Common.Tasks
{
    public abstract class Library
    {
        private string _configName;

        public string ConfigName {
            get {
                return _configName ?? this.GetType ().Name;
            }
            set {
                _configName = value;
            }
        }

        private FileSystems _fs;

        protected FileSystems fs {
            get {
                return _fs = _fs ?? new FileSystems {
                    Config = new FileSystem (library: this, type: FileSystemType.Config),
                    Runtime = new FileSystem (library: this, type: FileSystemType.Runtime)
                };
            }
        }

        public FileSystems FileSystems { get { return fs; } }
    }
}

