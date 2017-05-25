using System;
using System.Data;
using System.Data.SqlClient;

namespace CoPilot.ORM.Database.Commands
{
    public abstract class UnitOfWork : IDisposable
    {
        private readonly string _transactionId;
        
        private bool _isCommited;
        protected SqlTransaction Transaction { get; }
        protected SqlConnection SqlConnection { get; }
        protected SqlCommand SqlCommand { get; }

        protected UnitOfWork(SqlConnection connection, IsolationLevel isolation = IsolationLevel.ReadCommitted, int timeout = 30)
        {
            _transactionId = "T"+DateTime.Now.ToFileTime();

            SqlConnection = connection;
            SqlConnection.Open();
            Transaction = connection.BeginTransaction(isolation, _transactionId);
            SqlCommand = new SqlCommand()
            {
                Connection = SqlConnection,
                Transaction = Transaction,
                CommandTimeout = timeout
            };
        }

        public void Commit()
        {
            if (_isCommited) return;

            Transaction.Commit();
            _isCommited = true;
        }

        public void Rollback()
        {
            Transaction.Rollback();            
        }

        public void Dispose()
        {
            SqlConnection.Close();
            SqlCommand.Dispose();
            Transaction.Dispose();
            SqlConnection.Dispose();
        }

        public override string ToString()
        {
            return _transactionId;
        }
    }
}
