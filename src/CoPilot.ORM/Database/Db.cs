using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq.Expressions;
using CoPilot.ORM.Common;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Mapping;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Database
{
    internal class Db :  IDb
    {
        private readonly string _connectionString;

        internal Db(DbModel model, string connectionString)
        {
            Model = model;
            _connectionString = connectionString;
            
        }

        public SqlConnection Connection => new SqlConnection(_connectionString);

        public DbModel Model { get; }

        public DbResponse Query(string commandText, object args, params string[] names)
        {
            using (var rdr = new DbReader(Connection, Model))
            {
                return rdr.Query(commandText, args, names);
            }
        }

        public IEnumerable<T> Query<T>(string commandText, object args, ObjectMapper mapper = null, params string[] names)
        {
            using (var rdr = new DbReader(Connection, Model))
            {
                return rdr.Query<T>(commandText, args, mapper, names);
            }
        }

        public IEnumerable<T> Query<T>(OrderByClause<T> orderBy, Predicates predicates, Expression<Func<T, bool>> filter = null, params string[] include) where T : class
        {
            using (var rdr = new DbReader(Connection, Model))
            {
                return rdr.Query(orderBy, predicates, filter, include);
            }
        }

        public IEnumerable<TDto> Query<TEntity, TDto>(Expression<Func<TEntity, object>> selector, OrderByClause<TEntity> orderByClause, Predicates predicates, Expression<Func<TEntity, bool>> filter = null) where TEntity : class
        {
            using (var rdr = new DbReader(Connection, Model))
            {
                return rdr.Query<TEntity, TDto>(selector, orderByClause, predicates, filter);
            }
        }
        
        public T FindByKey<T>(object key, params string[] include) where T : class
        {
            using (var reader = new DbReader(Connection, Model))
            {
                return reader.FindByKey<T>(key, include);
            }
        }

        public int Command(string commandText, object args = null)
        {
            var request = DbRequest.CreateRequest(Model, commandText, args);
            
            using (var con = Connection)
            {
                var command = new SqlCommand()
                {
                    Connection = con,
                    CommandTimeout = 0,
                    CommandType = request.CommandType
                };
                con.Open();
                var response = CommandExecutor.ExecuteNonQuery(command, request);
                con.Close();

                return response;
            }
        }
        
        public object Scalar(string commandText, object args = null)
        {
            var request = DbRequest.CreateRequest(Model, commandText, args);

            using (var con = Connection)
            {
                var command = new SqlCommand()
                {
                    Connection = con,
                    CommandTimeout = 0,
                    CommandType = request.CommandType
                };
                con.Open();
                var response = CommandExecutor.ExecuteScalar(command, request);
                con.Close();

                return response;
            }
        }

        public void Save<T>(T entity, OperationType operations, params string[] include) where T : class
        {
            using (var writer = new DbWriter(Model, Connection) { Operations = operations })
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
            using (var writer = new DbWriter(Model, Connection) { Operations = operations })
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
            using (var writer = new DbWriter(Model, Connection) { Operations = OperationType.Delete|OperationType.Update })
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
            using (var writer = new DbWriter(Model, Connection) { Operations = OperationType.Delete | OperationType.Update })
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
            using (var writer = new DbWriter(Model, Connection) { Operations = OperationType.Update})
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
            var validator = Model.ResourceLocator.Get<IModelValidator>();
            return validator.Validate(this);
        }     
    } 
}
