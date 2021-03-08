using System;
using System.IO;

namespace SqExpress.CodeGenUtil.Logger
{

    public interface ILogger
    {
        void LogMinimal(string message);
        void LogNormal(string message);
        void LogDetailed(string message);
        bool IsMinimalOrHigher { get; }
        bool IsNormalOrHigher { get; }
        bool IsDetailed { get; }
    }

    public class DefaultLogger : ILogger
    {
        private readonly TextWriter _out;

        private readonly Verbosity _verbosity;

        public DefaultLogger(TextWriter @out, Verbosity verbosity)
        {
            this._out = @out;
            this._verbosity = verbosity;
        }

        public void LogMinimal(string message)
        {
            if (this.IsMinimalOrHigher)
            {
                this._out.WriteLine(message);
            }
        }

        public void LogNormal(string message)
        {
            if (this.IsNormalOrHigher)
            {
                this._out.WriteLine(message);
            }
        }

        public void LogDetailed(string message)
        {
            if (this.IsDetailed)
            {
                this._out.WriteLine(message);
            }
        }

        public bool IsMinimalOrHigher => this._verbosity >= Verbosity.Minimal;
        public bool IsDetailed => this._verbosity >= Verbosity.Detailed;
        public bool IsNormalOrHigher => this._verbosity >= Verbosity.Normal;
    }


}