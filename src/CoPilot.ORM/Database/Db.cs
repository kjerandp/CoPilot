using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using CoPilot.ORM.Common;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Query;
using CoPilot.ORM.Database.Providers;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Mapping;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Database
{
    internal class Db :  IDb
    {
        private readonly string _connectionString;
 
        internal Db(IDbProvider provider, string connectionString, DbModel model)
        {
            DbProvider = provider;
            Model = model;
            _connectionString = connectionString;        
        }

        public IDbProvider DbProvider { get; }

        public IDbConnection Connection => DbProvider.CreateConnection(_connectionString);

        public DbModel Model { get; }

        public DbResponse Query(string commandText, object args, params string[] names)
        {
            using (var rdr = new DbReader(this))
            {
                return rdr.Query(commandText, args, names);
            }
        }

        public IEnumerable<T> Query<T>(string commandText, object args, params string[] names)
        {
            using (var rdr = new DbReader(this))
            {
                return rdr.Query<T>(commandText, args, null, names);
            }
        }

        public IEnumerable<T> Query<T>(string commandText, object args, ObjectMapper mapper, params string[] names)
        {
            using (var rdr = new DbReader(this))
            {
                return rdr.Query<T>(commandText, args, mapper, names);
            }
        }

        public IEnumerable<T> Query<T>(OrderByClause<T> orderBy, Predicates predicates, Expression<Func<T, bool>> filter = null, params string[] include) where T : class
        {
            using (var rdr = new DbReader(this))
            {
                return rdr.Query(orderBy, predicates, filter, include);
            }
        }

        public IEnumerable<TDto> Query<TEntity, TDto>(Expression<Func<TEntity, object>> selector, OrderByClause<TEntity> orderByClause, Predicates predicates, Expression<Func<TEntity, bool>> filter = null) where TEntity : class
        {
            using (var rdr = new DbReader(this))
            {
                return rdr.Query<TEntity, TDto>(selector, orderByClause, predicates, filter);
            }
        }
        
        public T FindByKey<T>(object key, params string[] include) where T : class
        {
            using (var reader = new DbReader(this))
            {
                return reader.FindByKey<T>(key, include);
            }
        }

        public int Command(string commandText, object args = null)
        {
            var request = DbRequest.CreateRequest(Model, commandText, args);
            
            using (var con = Connection)
            {
                var command = DbProvider.CreateCommand(con);
                command.CommandType = request.CommandType;
                request.Command = command;
                con.Open();
                var response = DbProvider.ExecuteNonQuery(request);
                con.Close();

                return response;
            }
        }
        
        public object Scalar(string commandText, object args = null)
        {
            var request = DbRequest.CreateRequest(Model, commandText, args);

            using (var con = Connection)
            {
                var command = DbProvider.CreateCommand(con);
                command.CommandType = request.CommandType;
                request.Command = command;
                con.Open();
                var response = DbProvider.ExecuteScalar(request);
                con.Close();

                return response;
            }
        }

        public T Scalar<T>(string commandText, object args = null)
        {
            object convertedValue;
            ReflectionHelper.ConvertValueToType(typeof(T), Scalar(commandText, args), out convertedValue);

            return (T)convertedValue;
        }

        public void Save<T>(T entity, params string[] include) where T : class
        {
            Save(entity, (OperationType.Insert | OperationType.Update | OperationType.Delete), include);
        }

       public void Save<T>(IEnumerable<T> entities, params string[] include) where T : class
        {
            Save(entities, (OperationType.Insert | OperationType.Update | OperationType.Delete), include);
        }

        public void Save<T>(T entity, OperationType operations, params string[] include) where T : class
        {
            using (var writer = new DbWriter(this) { Operations = operations })
            {
                try
                {
                    writer.Save(entity, include);
                    writer.Commit();
                }
                catch(Exception ex)
                {
                    writer.Rollback();
                    throw new CoPilotDataException("Unable to save entity!", ex);
                }        
            }

        }

        public void Save<T>(IEnumerable<T> entities, OperationType operations, params string[] include) where T : class
        {
            using (var writer = new DbWriter(this) { Operations = operations })
            {
                try
                {
                    writer.Save(entities, include);
                    writer.Commit();
                }
                catch (Exception ex)
                {
                    writer.Rollback();
                    throw new CoPilotDataException("Unable to save entity!", ex);
                }
            }
        }

        public void Delete<T>(T entity, params string[] include) where T : class
        {
            using (var writer = new DbWriter(this) { Operations = OperationType.Delete|OperationType.Update })
            {
                try
                {
                    writer.Delete(entity, include);
                    writer.Commit();
                }
                catch (Exception ex)
                {
                    writer.Rollback();
                    throw new CoPilotDataException("Unable to delete entity!", ex);
                }
            }
        }

        public void Delete<T>(IEnumerable<T> entities, params string[] include) where T : class
        {
            using (var writer = new DbWriter(this) { Operations = OperationType.Delete | OperationType.Update })
            {
                try
                {
                    writer.Delete(entities, include);
                    writer.Commit();
                }
                catch (Exception ex)
                {
                    writer.Rollback();
                    throw new CoPilotDataException("Unable to delete entity!", ex);
                }
            }
        }

        public void Patch<T>(object dto) where T : class
        {
            using (var writer = new DbWriter(this) { Operations = OperationType.Update})
            {
                try
                {
                    writer.Patch<T>(dto);
                    writer.Commit();
                }
                catch (Exception ex)
                {
                    writer.Rollback();
                    throw new CoPilotDataException("Unable to patch entity!", ex);
                }
            }
        }

        public bool ValidateModel()
        {
            return DbProvider.ValidateModel(this);
        }

        public IQuery<T> From<T>() where T : class
        {
            using (var rdr = new DbReader(this))
            {
                return rdr.From<T>();
            }
        }
    } 
}
