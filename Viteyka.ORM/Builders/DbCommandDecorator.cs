using System;
using System.Data;

namespace Viteyka.ORM.Builders
{
    internal class DbCommandDecorator : IDbCommand
    {
        private IDbCommand _command;

        private DbCommandDecorator(IDbCommand command)
        {
            if (command == null)
                throw new ArgumentNullException("command");

            _command = command;
        }

        public void Cancel()
        {
            _command.Cancel();
        }

        public string CommandText
        {
            get
            {
                return _command.CommandText;
            }
            set
            {
                _command.CommandText = value;
            }
        }

        public int CommandTimeout
        {
            get
            {
                return _command.CommandTimeout;
            }
            set
            {
                _command.CommandTimeout = value;
            }
        }

        public CommandType CommandType
        {
            get
            {
                return _command.CommandType;
            }
            set
            {
                _command.CommandType = value;
            }
        }

        public IDbConnection Connection
        {
            get
            {
                return _command.Connection;
            }
            set
            {
                _command.Connection = value;
            }
        }

        public IDbDataParameter CreateParameter()
        {
            return _command.CreateParameter();
        }

        public int ExecuteNonQuery()
        {
            NotificationPoint.Instance.Notify(_command, _command.CommandText);
            return _command.ExecuteNonQuery();
        }

        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            NotificationPoint.Instance.Notify(_command, _command.CommandText);
            return _command.ExecuteReader(behavior);
        }

        public IDataReader ExecuteReader()
        {
            NotificationPoint.Instance.Notify(_command, _command.CommandText);
            return _command.ExecuteReader();
        }

        public object ExecuteScalar()
        {
            NotificationPoint.Instance.Notify(_command, _command.CommandText);
            return _command.ExecuteScalar();
        }

        public IDataParameterCollection Parameters
        {
            get { return _command.Parameters; }
        }

        public void Prepare()
        {
            _command.Prepare();
        }

        public IDbTransaction Transaction
        {
            get
            {
                return _command.Transaction;
            }
            set
            {
                _command.Transaction = value;
            }
        }

        public UpdateRowSource UpdatedRowSource
        {
            get
            {
                return _command.UpdatedRowSource;
            }
            set
            {
                _command.UpdatedRowSource = value;
            }
        }

        public void Dispose()
        {
            _command.Dispose();
        }

        public IDbDataParameter AddParam(string name, object value)
        {
            var param = CreateParameter();
            param.ParameterName = String.IsNullOrWhiteSpace(name) ? String.Format("@param{0}", Parameters.Count) : name;
            param.Value = value;
            param.Direction = ParameterDirection.Input;
            Parameters.Add(param);
            return param;
        }

        public static DbCommandDecorator Create(IDbConnection connection, string text = null, CommandType type = CommandType.Text)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");

            if (connection.State == ConnectionState.Closed)
                connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandType = type;
            cmd.CommandText = text;
            return new DbCommandDecorator(cmd);
        }
    }
}
