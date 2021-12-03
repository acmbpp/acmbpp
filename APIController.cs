using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace BPP.CustomAPI
{
    public class APIController:IPlugin
    {

        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracing service
            ITracingService tracingService =
            (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.MessageName.Equals("bpp_GetOrganization") && context.Stage.Equals(30))
            {

                try
                {
                    string orgName = (string)context.InputParameters["bpp_OrgName"];

                    if (!string.IsNullOrEmpty(orgName))
                    {
                        //Crear conexion a DV en el contexto del usuario
                        IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                        IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                        try
                        {
                            QueryExpression q = new QueryExpression();
                            q.EntityName = "mnst_organization";
                            q.ColumnSet = new ColumnSet(true);
                            q.Criteria.AddCondition(new ConditionExpression("mnst_name", ConditionOperator.Equal, orgName));

                            EntityCollection organizations = service.RetrieveMultiple(q);

                            q = new QueryExpression();
                            q.EntityName = "mnst_environment";
                            q.ColumnSet = new ColumnSet(true);
                            EntityCollection environments = service.RetrieveMultiple(q);

                            q = new QueryExpression();
                            q.EntityName = "mnst_powerapp";
                            q.ColumnSet = new ColumnSet(true);
                            EntityCollection apps = service.RetrieveMultiple(q);

                            EntityCollection response = new EntityCollection();
                            if (organizations != null && organizations.Entities.Count > 0)
                            {
                                
                                //response = results.Entities;
                                foreach (var org in organizations.Entities)
                                {
                                    
                                    var organization = new Entity();
                                    organization["Name"] = org.GetAttributeValue<string>("mnst_name");
                                    organization["Id"] = org.Id;

                                    var envList = environments.Entities.Where(e => e.GetAttributeValue<EntityReference>("mnst_organization").Id.Equals(org.Id)).Select(e =>
                                    {
                                        //Datos del entorno
                                        var env = new Entity();
                                        env["envName"] = e.GetAttributeValue<string>("mnst_name");
                                        env["HasDataverse"] = e.GetAttributeValue<bool>("mnst_hasdataverse");
                                        env["envId"] = e.Id;

                                        //Lista de Apps
                                        var applist = apps.Entities.Where(a => a.GetAttributeValue<EntityReference>("mnst_environment").Id.Equals(e.Id)).Select(a =>
                                        {
                                            var app = new Entity();
                                            app["appName"] = a.GetAttributeValue<string>("mnst_name");
                                            app["appCreatedOn"] = a.GetAttributeValue<DateTime>("createdon");
                                            return app;
                                        }).ToList();
                                        env["appListCount"] = applist.Count();
                                        env["appList"] = new EntityCollection(applist);

                                        //Return el nuevo objeto creado
                                        return env;
                                    }).ToList();

                                    response.Entities.Add(organization);
                                }

                            }
                            context.OutputParameters["bpp_Organization"] = response;

                        }
                        catch (Exception)
                        {

                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    tracingService.Trace("mnst_GetEnvironmentsByOrganizationName: {0}", ex.ToString());
                    throw new InvalidPluginExecutionException("An error occurred in mnst_GetEnvironmentsByOrganizationName.", ex);
                }
            }
            else
            {
                tracingService.Trace("mnst_GetEnvironmentsByOrganizationName plug-in is not associated with the expected message or is not registered for the main operation.");
            }
        }
    }
}
