using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Shell.Common.Tasks;
using Shell.Common.Util;

namespace Shell.Common.IO
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
    }
}

