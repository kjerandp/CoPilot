using System;
using System.Data;
using System.Data.SqlClient;

namespace CoPilot.ORM.Database.Commands
{
    public class UnitOfWork : IDisposable
    {
        private readonly string _transactionId;
        
        private bool _isCommited;
        public SqlTransaction Transaction { get; }
        public SqlConnection Connection { get; }
        public SqlCommand Command { get; }

        public UnitOfWork(SqlConnection connection, IsolationLevel isolation = IsolationLevel.ReadCommitted, int timeout = 30)
        {
            _transactionId = "T"+DateTime.Now.ToFileTime();
            Connection = connection;
            Connection.Open();
            Transaction = connection.BeginTransaction(isolation, _transactionId);
            Command = new SqlCommand()
            {
                Connection = Connection,
                Transaction = Transaction,
                CommandTimeout = timeout
            };
        }

        public void Commit()
        {
            if (!_isCommited)
            {
                Transaction.Commit();
                _isCommited = true;
            }          
        }

        public void Rollback()
        {
            Transaction.Rollback();            
        }

        public void Dispose()
        {
            Connection.Close();
            Command.Dispose();
            Transaction.Dispose();
            Connection.Dispose();
        }

        public override string ToString()
        {
            return _transactionId;
        }
    }
}
