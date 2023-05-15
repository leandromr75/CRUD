using SOP.Backoffice.Helpers;
using SOP.Backoffice.Models;
using SOP.Backoffice.Models.DTO;
using SOP.Backoffice.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Script.Serialization;

namespace SOP.Backoffice.Controllers
{
    public class AppController : ApiController
    {
        private readonly bool debug = true;
        private readonly string token = "key_tecnologia_app_sop";
        public DAO db = new DAO();
        

        [HttpPost]
        [Route("api/login")]
        public async Task<HttpResponseMessage> Login([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/Login", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (string.IsNullOrEmpty(m.Email) || string.IsNullOrEmpty(m.Senha))
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
                }

                var usuario = (from user in db.PessoaUsuario
                               where user.Email == m.Email && user.Senha == m.Senha && user.Ativo && !user.Removido
                               select new
                               {
                                   user.Id,
                                   user.Email,
                                   user.Nome,
                                   user.DataCriacao,
                                   user.DataUltimoLogin,
                               }).FirstOrDefault();

                if (usuario != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, usuario);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/Login", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/getMenuUsuarioProcesso")]
        public async Task<HttpResponseMessage> GetMenuUsuarioProcesso([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                if (debug) new Logging().AddError(json, "Api/getMenuUsuarioProcesso", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token || m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var areas = (from areaUsuario in db.AreaPessoaUsuario
                             where areaUsuario.IdPessoaUsuario == m.Id && areaUsuario.Area.TipoArea == AreaTipoEnum.Processos && !areaUsuario.Removido && !areaUsuario.Area.Removido
                             select new
                             {

                                 Id = areaUsuario.IdArea,
                                 Nome = areaUsuario.Area.Descricao
                             }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, areas);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/getMenuUsuario");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/getTarefaProcesso")]
        public async Task<HttpResponseMessage> GetTarefaProcesso([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (StringCipher.Decrypt(m.Token) != token || m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var areas = (from areaChecklist in db.AreaChecklistItem
                             where areaChecklist.IdPessoaUsuario == m.IdUsuario && areaChecklist.Area.TipoArea == AreaTipoEnum.Processos && !areaChecklist.Removido && !areaChecklist.Area.Removido
                             select new
                             {

                                 Id = areaChecklist.IdChecklistItem,
                                 Nome = areaChecklist.ChecklistItem.Descricao,
                                 areaChecklist.DataInicio
                             }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, areas);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/getMenuUsuario");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        #region Tarefas Usuarios
        [HttpPost]
        [Route("api/getTarefasUsuarioAgrupadas")]
        public async Task<HttpResponseMessage> GetTarefasUsuarioAgrupadas([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (StringCipher.Decrypt(m.Token) != token || m.IdUsuario == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var tasks = (from tarefa in db.TarefaUsuario.Where(w => w.IdUsuario == m.IdUsuario && !w.Removido).ToList()
                             orderby tarefa.DataAgendamentoTarefa
                             select new
                             {

                                 Data = tarefa.DataAgendamentoTarefa.ToString("dd/MM/yyyy")
                             }).Distinct().ToList();

                return Request.CreateResponse(HttpStatusCode.OK, tasks);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/getTarefasUsuarioAgrupadas");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/getTarefasUsuario")]
        public async Task<HttpResponseMessage> GetTarefasUsuario([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (StringCipher.Decrypt(m.Token) != token || m.IdUsuario == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var tasks = (from tarefa in db.TarefaUsuario.Where(w => w.IdUsuario == m.IdUsuario && !w.Removido).ToList()
                             where tarefa.DataAgendamentoTarefa.ToString("dd/MM/yyyy") == m.Data
                             orderby tarefa.DataFinalizacao, tarefa.DataInicio
                             select new
                             {

                                 tarefa.Id,
                                 Descricao = tarefa.Descricao,
                                 tarefa.DataAgendamentoTarefa,
                                 tarefa.DataInicio,
                                 DataConclusao = tarefa.DataFinalizacao,
                                 IdArea = tarefa.IdArea ?? 0,
                                 Area = tarefa.IdArea > 0 ? (tarefa.Area.TipoArea.ToString() + " / " + tarefa.Area.Descricao) : "",
                                 IdObra = tarefa.IdObra ?? 0,
                                 Obra = tarefa.Obra?.Nome ?? "",
                                 NivelAtencao = tarefa.NivelAtencao.ToString(),
                                 Prioridade = tarefa.Prioridade.ToString(),
                                 PodeExcluirEditarTarefa = tarefa.IdUsuario == m.IdUsuario,
                                 tarefa.AvisoData,
                                 AvisoHoras = (int)tarefa.AvisoHoras
                             }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, tasks);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/getTarefasUsuario");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/getTarefasUsuarioConcluidas")]
        public async Task<HttpResponseMessage> GetTarefasUsuarioConcluidas([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (StringCipher.Decrypt(m.Token) != token || m.IdUsuario == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var tasks = (from tarefa in db.TarefaUsuario.Where(w => w.IdUsuario == m.IdUsuario && !w.Removido).ToList()
                             where tarefa.DataFinalizacao != null 
                             orderby tarefa.DataFinalizacao, tarefa.DataInicio
                             select new
                             {

                                 tarefa.Id,
                                 Descricao = tarefa.Descricao,
                                 tarefa.DataAgendamentoTarefa,
                                 tarefa.DataInicio,
                                 DataConclusao = tarefa.DataFinalizacao,
                                 IdArea = tarefa.IdArea ?? 0,
                                 Area = tarefa.IdArea > 0 ? (tarefa.Area.TipoArea.ToString() + " / " + tarefa.Area.Descricao) : "",
                                 IdObra = tarefa.IdObra ?? 0,
                                 Obra = tarefa.Obra?.Nome ?? "",
                                 NivelAtencao = tarefa.NivelAtencao.ToString(),
                                 Prioridade = tarefa.Prioridade.ToString(),
                                 PodeExcluirEditarTarefa = tarefa.IdUsuario == m.IdUsuario,
                                 tarefa.AvisoData,
                                 AvisoHoras = (int)tarefa.AvisoHoras
                             }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, tasks);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/getTarefasUsuarioConcluidas");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/addTarefaUsuario")]
        public async Task<HttpResponseMessage> AddTarefaUsuario([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (StringCipher.Decrypt(m.Token) != token || m.IdUsuario == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var tarefaUsuario = new TarefaUsuario()
                {
                    DataCriacao = Helper.GetDateNow(),
                    Descricao = m.Descricao,
                    IdUsuario = m.IdUsuario,
                    NivelAtencao = (TarefaUsuarioNivelAtencaoEnum)m.NivelAtencao,
                    DataAgendamentoTarefa = m.DataTarefa,
                    AvisoHoras = (TarefaUsuarioAvisoEnum)m.AvisoHoras,
                    Prioridade = m.Prioridade,
                    AvisoData = m.DataTarefa.AddHours(-m.AvisoHoras)
                };

                if (m.IdArea > 0) tarefaUsuario.IdArea = m.IdArea;
                if (m.IdObra > 0) tarefaUsuario.IdObra = m.IdObra;

                db.TarefaUsuario.Add(tarefaUsuario);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/addTarefaUsuario");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/editaTarefaUsuario")]
        public async Task<HttpResponseMessage> EditaTarefaUsuario([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (StringCipher.Decrypt(m.Token) != token || m.IdUsuario == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var item = db.TarefaUsuario.Where(a => a.Id == m.Id).FirstOrDefault();
                item.Descricao = m.Descricao;
                item.NivelAtencao = (TarefaUsuarioNivelAtencaoEnum)m.NivelAtencao;
                item.DataAgendamentoTarefa = m.DataTarefa;
                item.AvisoHoras = (TarefaUsuarioAvisoEnum)m.AvisoHoras;
                item.Prioridade = m.Prioridade;
                item.AvisoData = m.DataTarefa.AddHours(-m.AvisoHoras);

                if (m.IdArea > 0)
                    item.IdArea = m.IdArea;
                else
                    item.IdArea = null;

                if (m.IdObra > 0)
                    item.IdObra = m.IdObra;
                else 
                    item.IdObra = null;

                db.Entry(item).State = EntityState.Modified;
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/editaTarefaUsuario");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/removeTarefaUsuario")]
        public async Task<HttpResponseMessage> RemoveTarefaUsuario([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (StringCipher.Decrypt(m.Token) != token || m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var item = db.TarefaUsuario.Where(a => a.Id == m.Id).FirstOrDefault();
                item.Removido = true;
                db.Entry(item).State = EntityState.Modified;
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/removeTarefaUsuario");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/iniciaTarefaUsuario")]
        public async Task<HttpResponseMessage> IniciaTarefaUsuario([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (StringCipher.Decrypt(m.Token) != token || m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var item = db.TarefaUsuario.Where(a => a.Id == m.Id).FirstOrDefault();

                item.DataInicio = Helper.GetDateNow();
                db.Entry(item).State = EntityState.Modified;
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/iniciaTarefaUsuario");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/finalizaTarefaUsuario")]
        public async Task<HttpResponseMessage> FinalizaTarefaUsuario([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (StringCipher.Decrypt(m.Token) != token || m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var item = db.TarefaUsuario.Where(a => a.Id == m.Id).FirstOrDefault();

                item.DataFinalizacao = Helper.GetDateNow();
                db.Entry(item).State = EntityState.Modified;
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/finalizaTarefaUsuario");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/getUsuarios")]
        public async Task<HttpResponseMessage> GetUsuarios([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var usuario = (from user in db.PessoaUsuario
                             where !user.Removido
                             select new
                             {
                                 user.Id,
                                 Nome = user.Nome
                             }).Distinct().OrderBy(o => o.Nome).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, usuario);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/getAreas");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }


        [HttpPost]
        [Route("api/getAreas")]
        public async Task<HttpResponseMessage> GetAreas([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (StringCipher.Decrypt(m.Token) != token && m.IdUsuario == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var areas = (from user in db.AreaPessoaUsuario
                             where !user.Removido && !user.Area.Removido && user.IdPessoaUsuario == m.IdUsuario
                             select new
                             {

                                 user.Area.Id,
                                 Nome = user.Area.TipoArea.ToString() + " / " +  user.Area.Descricao
                             }).Distinct().OrderBy(o => o.Nome).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, areas);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/getAreas");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }
        #endregion

        [HttpPost]
        [Route("api/getChecklistArea")]
        public async Task<HttpResponseMessage> GetChecklistArea([FromBody] ApiAdminViewModel m)
        {

            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (StringCipher.Decrypt(m.Token) != token || m.Id == 0 && m.IdUsuario == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }
                var usuario = db.PessoaUsuario.FirstOrDefault(x => x.Id == m.IdUsuario);
                var controle = db.TarefaUsuario.Where(x => !x.Removido && x.IdArea == m.Id && x.IdUsuario == m.IdUsuario).ToList();
                if (controle.Count <= 0)
                {
                    var areas = (from areaCheckItem in db.AreaChecklistItem
                                 where areaCheckItem.IdArea == m.Id && areaCheckItem.IdPessoaUsuario == m.IdUsuario && !areaCheckItem.Removido
                                 orderby areaCheckItem.DataConclusao, areaCheckItem.DataInicio
                                 select new
                                 {
                                     areaCheckItem.Id,
                                     areaCheckItem.ChecklistItem.Descricao,
                                     areaCheckItem.DataInicio,
                                     areaCheckItem.DataConclusao
                                 }).ToList();
                    return Request.CreateResponse(HttpStatusCode.OK, areas);
                }
                var controle2 = db.AreaChecklistItem.Where(x => !x.Removido && x.IdArea == m.Id).ToList();
                if (controle2.Count <= 0)
                {
                    var taref = (from tarefa in db.TarefaUsuario
                                 where tarefa.IdArea == m.Id && tarefa.IdUsuario == m.IdUsuario && !tarefa.Removido
                                 select new
                                 {
                                     tarefa.Id,
                                     tarefa.Descricao,
                                     tarefa.DataInicio,
                                     tarefa.DataFinalizacao
                                 }).ToList();
                    return Request.CreateResponse(HttpStatusCode.OK, taref);
                }

                var leftOuterJoinADM = from e in db.TarefaUsuario.Where(x => !x.Removido && x.IdArea == m.Id)
                                       join d in db.AreaChecklistItem on e.IdArea equals d.IdArea
                                       select new
                                       {
                                           Id = e.Id,
                                           Descricao = e.Descricao + " [" + e.Usuario.Nome + "]" ,
                                           DataInicio = e.DataInicio,
                                           DataConclusao = e.DataFinalizacao
                                       };


                var rightOuterJoinADM = from d in db.AreaChecklistItem
                                        join e in db.TarefaUsuario.Where(x => !x.Removido && x.IdArea == m.Id) on d.IdArea equals e.IdArea
                                        where d.IdArea == m.Id && d.IdPessoaUsuario == m.IdUsuario && !d.Removido
                                        orderby d.DataConclusao, d.DataInicio
                                        select new
                                        {
                                            Id = d.Id,
                                            Descricao = d.ChecklistItem.Descricao,
                                            DataInicio = d.DataInicio,
                                            DataConclusao = d.DataConclusao
                                        };
                
                if (!usuario.Admin)
                {
                    rightOuterJoinADM = from d in db.AreaChecklistItem.Where(x => x.IdPessoaUsuario == m.IdUsuario)
                                        join e in db.TarefaUsuario.Where(x => !x.Removido && x.IdArea == m.Id) on d.IdArea equals e.IdArea
                                        where d.IdArea == m.Id && d.IdPessoaUsuario == m.IdUsuario && !d.Removido
                                        orderby d.DataConclusao, d.DataInicio
                                        select new
                                        {
                                            Id = d.Id,
                                            Descricao = d.ChecklistItem.Descricao,
                                            DataInicio = d.DataInicio,
                                            DataConclusao = d.DataConclusao
                                        };
                }
                leftOuterJoinADM = leftOuterJoinADM.Union(rightOuterJoinADM);
                var retornoADM = leftOuterJoinADM.ToList();
                return Request.CreateResponse(HttpStatusCode.OK, retornoADM);

            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/getChecklistArea");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }

        }
        [HttpPost]
        [Route("api/getChecklistPavimento")]
        public async Task<HttpResponseMessage> GetChecklistPavimento([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                if (debug) new Logging().AddError(json, "Api/getChecklistPavimento", infos: m.DeviceInfos);

                var areas = (from tarefa in db.PessoaObraTarefa
                             where tarefa.Pavimento == m.Id
                             orderby tarefa.DataInicio, tarefa.DataTermino
                             select new
                             {

                                 tarefa.Id,
                                 tarefa.Tarefa.Descricao,
                                 tarefa.DataInicio,
                                 tarefa.DataTermino,
                                 OK = tarefa.DataTermino.HasValue
                             }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, areas);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/getChecklistPavimento");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/concluiChecklistPavimento")]
        public async Task<HttpResponseMessage> ConcluiChecklistPavimento([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                if (debug) new Logging().AddError(json, "Api/concluiChecklistPavimento", infos: m.DeviceInfos);

                var item = db.PessoaObraTarefa.Where(a => a.Id == m.Id).FirstOrDefault();

                item.DataInicio = Helper.GetDateNow();
                item.DataTermino = Helper.GetDateNow();
                db.Entry(item).State = EntityState.Modified;
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/concluiChecklistPavimento");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/iniciaChecklistArea")]
        public async Task<HttpResponseMessage> IniciaChecklistArea([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (StringCipher.Decrypt(m.Token) != token || m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var item = db.AreaChecklistItem.Where(a => a.Id == m.Id).FirstOrDefault();
                if (item != null)
                {
                    string s1 = item.ChecklistItem.Descricao;
                    string s2 = "[";
                    bool b = s1.Contains(s2);
                    if (!b)
                    {
                        item.DataInicio = Helper.GetDateNow();
                        db.Entry(item).State = EntityState.Modified;
                        db.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.OK, true);
                    }
                }

                var tu = db.TarefaUsuario.Where(x => x.Id == m.Id).FirstOrDefault();
                if (tu != null)
                {
                    tu.DataInicio = Helper.GetDateNow();
                    db.Entry(tu).State = EntityState.Modified;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, true);
                }
                
                var item2 = db.AreaChecklistItem.Where(a => a.Id == m.Id).FirstOrDefault();
                
                    item2.DataInicio = Helper.GetDateNow();
                    db.Entry(item).State = EntityState.Modified;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/iniciaChecklistArea");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/adicionaObservacaoArea")]
        public async Task<HttpResponseMessage> AdicionaObservacaoArea([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (StringCipher.Decrypt(m.Token) != token || m.IdArea == 0 || m.IdUsuario == 0 || string.IsNullOrEmpty(m.Msg))
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                db.AreaObservacao.Add(new AreaObservacao()
                {
                    IdArea = m.IdArea,
                    IdPessoaUsuario = m.IdUsuario,
                    DataCriacao = Helper.GetDateNow(),
                    Observacao = m.Msg
                });
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/adicionaObservacaoArea");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/finalizaChecklistArea")]
        public async Task<HttpResponseMessage> FinalizaChecklistArea([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (StringCipher.Decrypt(m.Token) != token || m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var item = db.AreaChecklistItem.Where(a => a.Id == m.Id).FirstOrDefault();
                if (item != null)
                {
                    string s1 = item.ChecklistItem.Descricao;
                    string s2 = "[";
                    bool b = s1.Contains(s2);
                    if (!b)
                    {
                        item.DataConclusao = Helper.GetDateNow();
                        db.Entry(item).State = EntityState.Modified;
                        db.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.OK, true);
                    }
                }

                var tu = db.TarefaUsuario.Where(x => x.Id == m.Id).FirstOrDefault();
                if (tu != null)
                {
                    tu.DataFinalizacao = Helper.GetDateNow();
                    db.Entry(tu).State = EntityState.Modified;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, true);
                }

                var item2 = db.AreaChecklistItem.Where(a => a.Id == m.Id).FirstOrDefault();
                item2.DataConclusao = Helper.GetDateNow();
                db.Entry(item).State = EntityState.Modified;
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/finalizaChecklistArea");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/assistenciaDetalhe")]
        public async Task<HttpResponseMessage> AssistenciaDetalhes([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (StringCipher.Decrypt(m.Token) != token || m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var assistencia = (from ass in db.PessoaObraAssistencia
                                   where ass.Id == m.Id
                                   select new
                                   {

                                       ass.Id,
                                       ass.Novo,
                                       ass.DataInicioAtendimento,
                                       ass.DataFinalAtendimento,
                                       ass.LocalAtendimento,
                                       ass.Carro,
                                       ass.PlacaCarro,
                                       ass.FormaPagamento,
                                       ass.ValorDeslocamento,
                                       ass.ValorTotal,
                                       ass.QtdeHrsAtendimento,
                                       ass.Detalhes,
                                       ass.Arquivo,
                                       ass.Arquivo2

                                   }).FirstOrDefault();

                return Request.CreateResponse(HttpStatusCode.OK, assistencia);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/assistenciaDetalhe");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/getEscopo")]
        public async Task<HttpResponseMessage> EscopoObra([FromBody] PessoaObraAssistencia m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var obra = db.PessoaObra.Where(a => a.Id == m.Id).FirstOrDefault();

                return Request.CreateResponse(HttpStatusCode.OK, obra.Escopo);

            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/getEscopo");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/alteraAssistencia")]
        public async Task<HttpResponseMessage> AlterarAssistencia([FromBody] PessoaObraAssistencia m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var assistencia = db.PessoaObraAssistencia.Where(a => a.Id == m.Id).FirstOrDefault();

                assistencia.Novo = m.Novo;
                assistencia.DataInicioAtendimento = m.DataInicioAtendimento;
                assistencia.DataFinalAtendimento = m.DataFinalAtendimento;
                assistencia.LocalAtendimento = m.LocalAtendimento;
                assistencia.Carro = m.Carro;
                assistencia.PlacaCarro = m.Carro;
                assistencia.FormaPagamento = m.FormaPagamento;
                assistencia.ValorDeslocamento = m.ValorDeslocamento;
                assistencia.ValorTotal = m.ValorTotal;
                assistencia.QtdeHrsAtendimento = m.QtdeHrsAtendimento;
                assistencia.Detalhes = m.Detalhes;

                db.Entry(assistencia).State = EntityState.Modified;
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/alteraAssistencia");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/assistenciaEdicao")]
        public async Task<HttpResponseMessage> AssistenciaEdicao([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (StringCipher.Decrypt(m.Token) != token || m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var assistencia = (from ass in db.PessoaObraAssistencia
                                   where ass.Id == m.Id
                                   select new
                                   {

                                       ass.Novo,
                                       ass.DataInicioAtendimento,
                                       ass.DataFinalAtendimento,
                                       ass.LocalAtendimento,
                                       ass.Carro,
                                       ass.PlacaCarro,
                                       ass.FormaPagamento,
                                       ass.ValorDeslocamento,
                                       ass.ValorTotal,
                                       ass.QtdeHrsAtendimento,
                                       ass.Detalhes,
                                       Servicos = (from assistenciaService in db.PessoaObraAssistenciaServico
                                                   where assistenciaService.IdObraAssistencia == ass.Id
                                                   select new
                                                   {

                                                       assistenciaService.Id,
                                                       assistenciaService.IdObraAssistencia,
                                                       assistenciaService.Nome,
                                                       assistenciaService.Valor,

                                                   }),
                                       Materiais = (from materiais in db.PessoaObraAssistenciaMateriais
                                                    where materiais.IdObraAssistencia == ass.Id
                                                    select new
                                                    {

                                                        materiais.Id,
                                                        materiais.IdObraAssistencia,
                                                        materiais.Nome,
                                                        materiais.ValorUnit,
                                                        materiais.Qtde,
                                                        materiais.UnidadeMedida

                                                    })

                                   }).FirstOrDefault();

                return Request.CreateResponse(HttpStatusCode.OK, assistencia);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/assistenciaEdicao");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/setAssistenciaServico")]
        public async Task<HttpResponseMessage> InsertServico([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (StringCipher.Decrypt(m.Token) != token || m.Id == 0 || string.IsNullOrEmpty(m.Nome) || m.Qtde == 0 || m.Valor == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                PessoaObraAssistenciaServico servico = new PessoaObraAssistenciaServico()
                {
                    IdObraAssistencia = m.Id,
                    Nome = m.Nome,
                    Qntd = m.Qtde,
                    Valor = m.Valor
                };

                db.PessoaObraAssistenciaServico.Add(servico);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/setServico");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/deleteServico")]
        public async Task<HttpResponseMessage> DeletarServico([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (StringCipher.Decrypt(m.Token) != token || m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var servico = db.PessoaObraAssistenciaServico.Where(s => s.Id == m.Id).FirstOrDefault();

                if (servico != null)
                {

                    db.PessoaObraAssistenciaServico.Remove(servico);
                    db.SaveChanges();

                }

                return Request.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/DeletarServico");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/deleteMaterial")]
        public async Task<HttpResponseMessage> DeletarMaterial([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (StringCipher.Decrypt(m.Token) != token || m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var material = db.PessoaObraAssistenciaMateriais.Where(s => s.Id == m.Id).FirstOrDefault();

                if (material != null)
                {

                    db.PessoaObraAssistenciaMateriais.Remove(material);
                    db.SaveChanges();

                }

                return Request.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/DeletarMaterial");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/setAssistenciaMaterial")]
        public async Task<HttpResponseMessage> InsertMaterial([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (StringCipher.Decrypt(m.Token) != token || m.Id == 0 || string.IsNullOrEmpty(m.Nome) || m.Qtde == 0 || m.ValorUnitario == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                PessoaObraAssistenciaMateriais material = new PessoaObraAssistenciaMateriais()
                {
                    IdObraAssistencia = m.Id,
                    Nome = m.Nome,
                    Qtde = m.Qtde,
                    UnidadeMedida = m.UnidadeMedida,
                    ValorUnit = m.ValorUnitario
                };

                db.PessoaObraAssistenciaMateriais.Add(material);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/setMaterial");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/setAssistencia")]
        public async Task<HttpResponseMessage> AddAssociacao([FromBody] ApiAdminViewModel m)
        {
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);

                if (StringCipher.Decrypt(m.Token) != token || string.IsNullOrEmpty(m.CNPJObra))
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var obra = db.PessoaObra.Where(o => o.CPF_CNPJ == m.CNPJObra).FirstOrDefault();

                Helper h = new Helper();

                if (obra == null)
                {

                    PessoaEmpresa pessoaEmpresa = new PessoaEmpresa()
                    {

                        DataCriacao = Helper.GetDateNow(),
                        DataAlteracao = Helper.GetDateNow(),
                        IdUsuarioCriacao = m.IdUsuarioCriacao

                    };

                    db.PessoaEmpresa.Add(pessoaEmpresa);
                    db.SaveChanges();

                    obra = new PessoaObra()
                    {
                        DataCriacao = Helper.GetDateNow(),
                        EnderecoNumero = m.Numero,
                        EnderecoCep = m.Cep,
                        Endereco = m.Rua,
                        IdUsuarioCriacao = m.IdUsuarioCriacao,
                        IdPessoaEmpresa = pessoaEmpresa.Id,
                        LancamentoPavimentoAutomatico = false,
                        Conclusao = false,
                        Ativo = true,
                        Removido = false,
                        PorcentagemConclusao = 0,
                        PorcentagemConclusaoEntrega = 0,
                        PorcentagemPavimentoAndamento = 0,
                        PorcentagemPavimentoChecklist = 0,
                        PorcentagemPavimentoConclusao = 0,
                        SomenteAssistencia = true
                    };

                    db.PessoaObra.Add(obra);
                    db.SaveChanges();

                }

                var pessoaUsuarioDaObra = db.PessoaUsuarioObra.Where(p => p.IdUsuario == m.IdUsuarioCriacao);

                if (pessoaUsuarioDaObra == null)
                {
                    PessoaUsuarioObra p = new PessoaUsuarioObra()
                    {

                        IdUsuario = m.IdUsuarioCriacao,
                        IdObra = obra.Id

                    };

                    db.PessoaUsuarioObra.Add(p);
                    db.SaveChanges();
                }

                PessoaObraAssistencia pessoaObraAssistencia = new PessoaObraAssistencia()
                {
                    Garantia = (m.Novo) ? false : true,
                    Novo = m.Novo,
                    DataCriacao = Helper.GetDateNow(),
                    Tipo = PessoaObraAssistenciaTipoEnum.Aberto,
                    QtdeHrsAtendimento = 0,
                    NotaAtendimentoSOP = 0,
                    ValorDeslocamento = 0,
                    ValorTotal = 0,
                    IdObra = obra.Id,
                    Responsavel = m.Responsavel,
                    Detalhes = m.Detalhes,
                    LocalAtendimento = "Rua: " + m.Rua + ", cep:" + m.Cep + ", numero: " + m.Numero,
                    IdUsuarioCriacao = m.IdUsuarioCriacao
                };

                string dirImages = HttpContext.Current.Server.MapPath($"~/ArquivosAssistencia/");
                if (!Directory.Exists(dirImages))
                {
                    Directory.CreateDirectory(dirImages);
                }

                var url = System.Configuration.ConfigurationManager.AppSettings["UrlWebSite"].ToString();

                if (m.Arquivo != null)
                {

                    var guid = Guid.NewGuid().ToString();
                    var foto1 = $@"{url}ArquivosAssistencia/{guid}.jpg";

                    var bytesImage1 = Convert.FromBase64String(m.Arquivo);
                    using (var imageFile = new FileStream($"{dirImages}{guid}.jpg", FileMode.Create))
                    {
                        imageFile.Write(bytesImage1, 0, bytesImage1.Length);
                        imageFile.Flush();
                    }

                    pessoaObraAssistencia.Arquivo = foto1;
                }

                if (m.Arquivo2 != null)
                {

                    var guid = Guid.NewGuid().ToString();
                    var foto2 = $@"{url}Arquivos/{guid}.jpg";

                    var bytesImage2 = Convert.FromBase64String(m.Arquivo2);
                    using (var imageFile = new FileStream($"{dirImages}{guid}.jpg", FileMode.Create))
                    {
                        imageFile.Write(bytesImage2, 0, bytesImage2.Length);
                        imageFile.Flush();
                    }

                    pessoaObraAssistencia.Arquivo2 = foto2;
                }


                db.PessoaObraAssistencia.Add(pessoaObraAssistencia);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/CadastroProduto");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/LoginCreate")]
        public async Task<HttpResponseMessage> LoginCreate([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/LoginCreate", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                using (var db = new DAO())
                {
                    var pessoaUsuario = new PessoaUsuario()
                    {
                        Nome = m.Nome,
                        Email = m.Email,
                        Senha = m.Senha,
                        DataCriacao = Helper.GetDateNow()
                    };
                    db.PessoaUsuario.Add(pessoaUsuario);
                    db.SaveChanges();
                }

                return Request.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/LoginCreate", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/LoginAlteracao")]
        public async Task<HttpResponseMessage> LoginAlteracao([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/LoginAlteracao", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                using (var db = new DAO())
                {
                    var pessoaUsuario = db.PessoaUsuario.FirstOrDefault(f => f.Id == m.Id);
                    if (pessoaUsuario != null)
                    {
                        pessoaUsuario.Nome = m.Nome;
                        pessoaUsuario.Email = m.Email;
                        pessoaUsuario.Senha = m.Senha;
                        db.Entry(pessoaUsuario).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/LoginAlteracao", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/SolicitacaoCreate")]
        public async Task<HttpResponseMessage> SolicitacaoCreate([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/SolicitacaoCreate", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado0");
                }

                if (m.Id == 0 || m.Tipo < 0 || string.IsNullOrEmpty(m.Msg) || string.IsNullOrEmpty(m.Title))
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado1");
                }

                using (var db = new DAO())
                {
                    var interacao = new Interacao()
                    {
                        IdTipo = m.Tipo, // 0 > Requisicao / 1 > Duvida / 2 > Reclamação / 3 > Geral
                        IdPessoaCriacao = m.Id,
                        IdObra = m.IdObra,
                        Detalhes = m.Msg,
                        Titulo = m.Title,
                        Status = InteracaoStatusEnum.Aberto,
                        DataCriacao = Helper.GetDateNow()
                    };
                    db.Interacao.Add(interacao);
                    db.SaveChanges();

                    return Request.CreateResponse(HttpStatusCode.OK, true);
                }

            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/SolicitacaoCreate", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/Solicitacoes")]
        public async Task<HttpResponseMessage> Solicitacoes([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/Solicitacoes", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var itens = (from inte in db.Interacao
                             where !inte.Removido && inte.IdObra == m.Id
                             select new
                             {
                                 inte.Id,
                                 inte.DataConclusao,
                                 inte.DataCriacao,
                                 inte.DataVencimento,
                                 inte.Detalhes,
                                 inte.Status,
                                 inte.Titulo,
                                 inte.IdTipo,
                                 inte.Tipo.Nome,
                             }).ToList();

                if (itens != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, itens);
                }

                return Request.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/Solicitacoes", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/notificacoes")]
        public async Task<HttpResponseMessage> Notificacoes([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/notificacoes", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var itens = (from noti in db.Notificacao
                             orderby noti.Lida, noti.Data descending
                             where noti.IdUsuario == m.Id
                             select new
                             {
                                 noti.Id,
                                 noti.Mensagem,
                                 noti.Tipo,
                                 noti.Lida,
                                 noti.IdInterno,
                                 noti.Data
                             }).ToList();

                if (itens != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, itens);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/notificacoes", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/notificacaoLida")]
        public async Task<HttpResponseMessage> NotificacaoLida([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/notificacaoLida", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                using (var db = new DAO())
                {
                    var item = db.Notificacao.FirstOrDefault(f => f.Id == m.Id);
                    if (item != null)
                    {
                        item.Lida = true;
                        db.Entry(item).State = EntityState.Modified;
                        db.SaveChanges();


                        return Request.CreateResponse(HttpStatusCode.OK, true);
                    }
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/notificacoes", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/andamentoInicio")]
        public async Task<HttpResponseMessage> AndamentoInicio([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/andamentoInicio", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                using (var db = new DAO())
                {
                    var item = db.PessoaObraAndamento.FirstOrDefault(f => f.Id == m.Id);
                    if (item != null)
                    {
                        item.DataInicio = Helper.GetDateNow();
                        db.Entry(item).State = EntityState.Modified;
                        db.SaveChanges();

                        Helper.RecalculateConclusaoObra(db, item.IdObra);

                        return Request.CreateResponse(HttpStatusCode.OK, true);
                    }
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/andamentoInicio", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/andamentoConcluir")]
        public async Task<HttpResponseMessage> AndamentoConcluir([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/andamentoConcluir", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                using (var db = new DAO())
                {
                    var item = db.PessoaObraAndamento.FirstOrDefault(f => f.Id == m.Id);
                    if (item != null)
                    {
                        item.DataConclusao = Helper.GetDateNow();
                        db.Entry(item).State = EntityState.Modified;
                        db.SaveChanges();

                        Helper.RecalculateConclusaoObra(db, item.IdObra);

                        return Request.CreateResponse(HttpStatusCode.OK, true);
                    }
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/andamentoConcluir", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/visita")]
        public async Task<HttpResponseMessage> Visita([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/visita", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var itens = (from visita in db.PessoaObraVisita
                             where visita.Id == m.Id
                             select new
                             {
                                 visita.Id,
                                 visita.UsuarioAbertura,
                                 visita.SolicitanteVisita,
                                 visita.DataVisitaAgendada,
                                 visita.MotivoVisita,
                                 visita.DataVisitaConcluida,
                                 visita.Observacoes
                             }).ToList();

                if (itens != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, itens);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/visita", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/alteraVisita")]
        public async Task<HttpResponseMessage> AlteraVisita([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/alteraVisita", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0 || string.IsNullOrEmpty(m.Msg))
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                using (var db = new DAO())
                {
                    var item = db.PessoaObraVisita.FirstOrDefault(f => f.Id == m.Id && f.DataVisitaConcluida == null);
                    if (item != null)
                    {
                        item.DataVisitaConcluida = Helper.GetDateNow();
                        item.Observacoes = m.Msg;
                        db.Entry(item).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/alteraVisita", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/visitaNova")]
        public async Task<HttpResponseMessage> VisitaNova([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/visita", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var itens = (from visita in db.PessoaObraVisita
                             where visita.IdObra == m.Id && visita.DataVisitaConcluida == null
                             select new
                             {
                                 visita.Id,
                                 visita.UsuarioAbertura,
                                 visita.SolicitanteVisita,
                                 visita.DataVisitaAgendada,
                                 visita.MotivoVisita,
                                 visita.DataVisitaConcluida,
                                 visita.Observacoes
                             }).ToList();

                if (itens != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, itens);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/visita", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/alteraEntrega")]
        public async Task<HttpResponseMessage> AlteraEntrega([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/alteraEntrega", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                using (var db = new DAO())
                {
                    var item = db.PessoaObraEntrega.FirstOrDefault(f => f.Id == m.Id && !f.Entregue);
                    if (item != null)
                    {
                        if (m.Checklist != null && m.Checklist.Any())
                        {
                            foreach (var itemCheck in m.Checklist)
                            {
                                var itemCk = db.PessoaObraEntregaChecklist.FirstOrDefault(f => f.Id == itemCheck.Id);
                                if (itemCk != null)
                                {
                                    itemCk.OK = itemCheck.OK;
                                    db.Entry(itemCk).State = EntityState.Modified;
                                    db.SaveChanges();
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(m.Msg) && !db.PessoaObraEntregaChecklist.Any(f => f.IdObraEntregue == m.Id && !f.OK))
                        {
                            item.DataConcluidaEntrega = Helper.GetDateNow();
                            item.Observacoes = m.Msg;
                            item.Entregue = true;
                            db.Entry(item).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/alteraEntrega", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/duvidas")]
        public async Task<HttpResponseMessage> Duvidas([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/duvidas", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var itens = (from duv in db.CentralDuvida
                             where !duv.Removido
                             select new
                             {
                                 duv.Id,
                                 duv.Nome,
                                 duv.Detalhes,
                             }).ToList();

                if (itens != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, itens);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/duvidas", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/obras")]
        public async Task<HttpResponseMessage> Obras([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/Obras", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var obra2 = db.PessoaObra.Where(f => !f.Removido).ToList();
                if (obra2.Count > 0)
                {
                    foreach (var obra in obra2)
                    {
                        var pavimentos = db.PessoaObraAndamento.Where(w => w.IdObra == obra.Id).ToList();
                        var pavQtde = pavimentos.Count();
                        var pavIniciar = pavimentos.Where(w => w.DataInicio == null).Count();
                        var pavEmAndamento = pavimentos.Where(w => w.DataInicio != null && w.DataConclusao == null).Count();
                        var pavConclusao = pavimentos.Where(w => w.DataConclusao != null).Count();
                        //var conclusaoChecklist = Convert.ToInt32(obra.PorcentagemPavimentoChecklist * .1);
                        //var conclusaoPavimento = Convert.ToInt32(obra.PorcentagemPavimentoConclusao * .9);


                        if (pavQtde > 0)
                        {
                            obra.PavimentoAIniciar = (100 * pavIniciar) / pavQtde;
                            obra.PavimentoEmAndamento = (100 * pavEmAndamento) / pavQtde;
                            obra.PavimentoConcluido = (100 * pavConclusao) / pavQtde;
                        }

                        var conclusaoPavimento = Convert.ToInt32(obra.PavimentoConcluido * .9);
                        var conclusaoChecklist = Convert.ToInt32(obra.PavimentoConcluido * .1);
                        
                        obra.PorcentagemPavimentoChecklist = conclusaoChecklist;
                        obra.PorcentagemConclusaoEntrega = conclusaoPavimento - obra.ConclusaoChecklist;
                        obra.PorcentagemPavimentoConclusao = conclusaoPavimento - obra.ConclusaoChecklist;
                        obra.PorcentagemPavimentoAndamento = 100 - conclusaoChecklist - conclusaoPavimento;
                        obra.PorcentagemConclusao = 100 - obra.PorcentagemPavimentoAndamento;
                    }
                }

                var itens = (from obra in obra2
                             //join obraUsuario in db.PessoaUsuarioObra on obra.Id equals obraUsuario.IdObra
                             where !obra.Removido
                             orderby obra.Nome
                             select new
                             {
                                 obra.Id,
                                 obra.Nome,
                                 obra.Avatar,
                                 obra.PorcentagemConclusao,
                                 obra.PorcentagemConclusaoEntrega,
                                 obra.PorcentagemPavimentoAndamento,
                                 obra.PorcentagemPavimentoConclusao,
                                 obra.PorcentagemPavimentoChecklist
                             }).ToList();

                

                if (itens != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, itens);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/Obras", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/atualizaObras")]
        public async Task<HttpResponseMessage> AtualizaObras([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/atualizaObras", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var itens = (from obra in db.PessoaObra
                             where obra.Id == m.Id
                             orderby obra.Nome
                             select new
                             {
                                 obra.Id,
                                 obra.Nome,
                                 obra.Avatar,
                                 obra.PorcentagemConclusao,
                                 obra.PorcentagemConclusaoEntrega,
                                 obra.PorcentagemPavimentoAndamento,
                                 obra.PorcentagemPavimentoConclusao,
                                 obra.PorcentagemPavimentoChecklist
                             }).ToList();

                if (itens != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, itens);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/atualizaObras", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/obraProjetos")]
        public async Task<HttpResponseMessage> ObraProjetos([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/obraProjetos", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var itens = (from obraProj in db.PessoaObraProjeto
                             where obraProj.IdObra == m.Id && !obraProj.Removido
                             select new
                             {
                                 obraProj.Id,
                                 obraProj.Arquivo,
                                 obraProj.Observacoes
                             }).ToList();

                if (itens != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, itens);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/obraProjetos", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/obraFaturamentos")]
        public async Task<HttpResponseMessage> ObraFaturamentos([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/obraFaturamentos", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var dataInicio = m.DataInicial.Date;
                var dataFim = m.DataFinal.AddHours(23);

                var itens = (from fat in db.PessoaObraRecebimento
                             where fat.IdObra == m.Id && fat.DataInicial >= dataInicio && fat.DataFinal <= dataFim
                             select new
                             {
                                 fat.Id,
                                 fat.Observacao,
                                 fat.DataInicial,
                                 fat.DataFinal,
                                 fat.Valor
                             }).ToList();

                if (itens != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, itens);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/obraFaturamentos", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/obraDetalhes")]
        public async Task<HttpResponseMessage> ObraDetalhes([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/obraDetalhes", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var item = (from obra in db.PessoaObra
                            where obra.Id == m.Id
                            select new
                            {
                                obra.Id,
                                obra.Nome,
                                obra.RazaoSocial,
                                obra.Escopo,
                                obra.Observacao,
                                obra.DetalhesProjeto,
                                obra.Avatar,
                                obra.QtdePavimentos,
                            }).FirstOrDefault();

                if (item != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, item);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/obraDetalhes", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/obraEstoque")]
        public async Task<HttpResponseMessage> ObraEstoque([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/obraEstoque", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var itens = (from item in db.PessoaObraEstoque
                             where item.IdObra == m.Id && item.QtdeAtual > 0
                             select new
                             {
                                 item.Id,
                                 Produto = item.Produto.Nome,
                                 Qtde = item.QtdeAtual
                             }).ToList();

                if (itens != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, itens);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/obraEstoque", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/getArquivos")]
        public async Task<HttpResponseMessage> GetArquivos([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/getArquivos", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0 || m.Tipo < 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                /*
                    * 
                        Visita = 0,
                        EditarRequisicao = 1,
                        Entrega = 2,
                        Projeto = 3
                    */

                switch (m.Tipo)
                {
                    case 0:

                        #region Arquivos Visita
                        var itensVisita = (from i in db.PessoaObraVisitaFoto
                                           where !i.Removido && i.IdObraVisita == m.Id
                                           select new
                                           {
                                               i.Id,
                                               i.Arquivo
                                           }).ToList();

                        if (itensVisita != null)
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, itensVisita);
                        }

                        break;
                    #endregion

                    case 1:

                        //ok

                        #region Arquivos Interacao / Solicitacao
                        var itens = (from i in db.InteracaoArquivo
                                     where !i.Removido && i.IdInteracao == m.Id
                                     select new
                                     {
                                         i.Id,
                                         i.Arquivo,
                                     }).ToList();

                        if (itens != null)
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, itens);
                        }

                        break;
                    #endregion

                    case 2:



                        #region Arquivos Entrega
                        var itensEN = (from i in db.PessoaObraEntregaArquivo
                                       where i.IdObraEntregue == m.Id
                                       select new
                                       {
                                           i.Id,
                                           Arquivo = i.Imagem,
                                       }).ToList();

                        if (itensEN != null)
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, itensEN);
                        }

                        break;
                    #endregion

                    case 3:

                        //ok
                        #region Arquivos Projeto 
                        var itensP = (from i in db.PessoaObraProjeto
                                      where i.IdObra == m.Id
                                      select new
                                      {
                                          i.Id,
                                          i.Arquivo,
                                      }).ToList();

                        if (itensP != null)
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, itensP);
                        }

                        break;
                        #endregion

                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/getArquivos", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/enviaArquivos")]
        public async Task<HttpResponseMessage> EnviaArquivos()
        {
            string deviceInfos = "", json = "";
            try
            {
                json = HttpContext.Current.Request.Params.ToString();
                if (debug) new Logging().AddError(json, "Api/enviaProjeto", infos: deviceInfos);

                var Id = 0;
                if (HttpContext.Current.Request.Params["IdObra"] != null)
                    int.TryParse(HttpContext.Current.Request.Params["IdObra"].ToString(), out Id);

                var tipoArquivo = 0;
                if (HttpContext.Current.Request.Params["Tipo"] != null)
                    int.TryParse(HttpContext.Current.Request.Params["Tipo"].ToString(), out tipoArquivo);

                var foto = "";
                if (HttpContext.Current.Request.Params["Foto"] != null)
                {
                    try
                    {
                        var id = Id;
                        string dirImages = HttpContext.Current.Server.MapPath($"~/Arquivos/");
                        if (!Directory.Exists(dirImages))
                        {
                            Directory.CreateDirectory(dirImages);
                        }

                        var fotoBase64 = HttpContext.Current.Request.Params["Foto"].ToString();
                        if (!string.IsNullOrEmpty(fotoBase64))
                        {
                            var url = System.Configuration.ConfigurationManager.AppSettings["UrlWebSite"].ToString();
                            var guid = Guid.NewGuid().ToString();
                            foto = $@"{url}Arquivos/{id}-{guid}.jpg";
                            var bytes = Convert.FromBase64String(fotoBase64);
                            using (var imageFile = new FileStream($"{dirImages}{id}-{guid}.jpg", FileMode.Create))
                            {
                                imageFile.Write(bytes, 0, bytes.Length);
                                imageFile.Flush();
                            }

                            /*
                                Visita = 0,
                                EditarRequisicao = 1,
                                Entrega = 2,
                                Projeto = 3
                                ProcessoArea = 4
                             */

                            switch (tipoArquivo)
                            {
                                case 0:



                                    #region Arquivos Visita
                                    using (var db = new DAO())
                                    {
                                        var visita = db.PessoaObraVisita.FirstOrDefault(f => f.Id == Id);
                                        if (visita != null)
                                        {
                                            db.PessoaObraVisitaFoto.Add(new PessoaObraVisitaFoto()
                                            {
                                                Arquivo = foto,
                                                DataCriacao = Helper.GetDateNow(),
                                                IdObraVisita = visita.Id,
                                            });
                                            db.SaveChanges();
                                        }
                                    }
                                    break;
                                #endregion

                                case 1:

                                    #region Arquivos Interacao / Solicitacao
                                    using (var db = new DAO())
                                    {
                                        db.InteracaoArquivo.Add(new InteracaoArquivo()
                                        {
                                            Arquivo = foto,
                                            IdInteracao = Id,
                                            DataCriacao = Helper.GetDateNow()
                                        });
                                        db.SaveChanges();
                                    }
                                    break;
                                #endregion

                                case 2:

                                    #region Arquivos Entrega
                                    using (var db = new DAO())
                                    {
                                        db.PessoaObraEntregaArquivo.Add(new PessoaObraEntregaArquivo()
                                        {
                                            Imagem = foto,
                                            IdObraEntregue = Id,
                                        });
                                        db.SaveChanges();
                                    }
                                    break;
                                #endregion

                                case 3:

                                    #region Arquivos Projeto
                                    using (var db = new DAO())
                                    {

                                        db.PessoaObraProjeto.Add(new PessoaObraProjeto()
                                        {
                                            Arquivo = foto,
                                            DataCriacao = Helper.GetDateNow(),
                                            IdObra = id
                                        });
                                        db.SaveChanges();
                                    }
                                    break;
                                #endregion

                                case 4:

                                    #region Arquivos Projeto
                                    using (var db = new DAO())
                                    {
                                        db.AreaDocumento.Add(new AreaDocumento()
                                        {
                                            ArquivoBase64 = foto,
                                            DataCriacao = Helper.GetDateNow(),
                                            NomeDocumentoOriginal = foto,
                                            NomeDocumento = foto,
                                            IdArea = id
                                        });
                                        db.SaveChanges();
                                    }
                                    break;
                                    #endregion

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        new Logging().AddError(ex, "Api/enviaProjeto");
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, true);

            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/enviaProjeto", infos: deviceInfos + " / " + json);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/removeVisitaArquivo")]
        public async Task<HttpResponseMessage> RemoveVisitaArquivo([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/removeVisitaArquivo", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                using (var db = new DAO())
                {
                    var visitaObraArquivo = db.PessoaObraVisitaFoto.FirstOrDefault(f => f.Id == m.Id);
                    visitaObraArquivo.Removido = true;
                    db.Entry(visitaObraArquivo).State = EntityState.Modified;
                    db.SaveChanges();
                }

                return Request.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/removeVisitaArquivo", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/assistenciaEdit")]
        public async Task<HttpResponseMessage> EditAssistencia([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/assistenciaEdit", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token || m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var assistencia = db.PessoaObraAssistencia.Where(a => a.Id == m.Id).FirstOrDefault();

                if (assistencia != null)
                {

                    assistencia.Detalhes = m.Detalhes;

                    db.Entry(assistencia).State = EntityState.Modified;
                    db.SaveChanges();

                    return Request.CreateResponse(HttpStatusCode.OK, true);

                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/assistenciaDetalhe", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }


        [HttpPost]
        [Route("api/assistencias")]
        public async Task<HttpResponseMessage> GetAssistencias([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/assistencias", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token || m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var assistencias = (from ass in db.PessoaObraAssistencia
                                    where ass.IdUsuarioCriacao == m.Id
                                    select new
                                    {
                                        ass.Id,
                                        ass.DataCriacao,
                                        ass.Tipo
                                    }).ToList();

                if (assistencias != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, assistencias);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/obraAndamentos", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/obraAndamentos")]
        public async Task<HttpResponseMessage> ObraAndamentos([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/obraAndamentos", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var itens = (from obraAndamento in db.PessoaObraAndamento
                             where obraAndamento.IdObra == m.Id
                             select new
                             {
                                 obraAndamento.Id,
                                 obraAndamento.DataInicio,
                                 obraAndamento.DataEntrega,
                                 obraAndamento.DataConclusao,
                                 obraAndamento.ObraServico.Servico.Nome,
                                 obraAndamento.IndexPavimento
                             }).OrderBy(x => x.IndexPavimento).ToList();

                if (itens != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, itens);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/obraAndamentos", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/obraCronogramas")]
        public async Task<HttpResponseMessage> ObraCronogramas([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/obraCronogramas", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var itens = (from crono in db.PessoaObraCronograma
                             where crono.IdObra == m.Id
                             select new
                             {
                                 crono.Id,
                                 crono.DataCriacao,
                                 crono.DataPrevisaoConclusao,
                                 crono.Nome
                             }).ToList();

                if (itens != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, itens);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/obraCronogramas", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/entrega")]
        public async Task<HttpResponseMessage> Entrega([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/entrega", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var itens = (from entrega in db.PessoaObraEntrega
                             where entrega.Id == m.Id
                             select new
                             {
                                 entrega.Id,
                                 entrega.UsuarioAbertura,
                                 entrega.DataAgendadaEntrega,
                                 entrega.Entregue,
                                 entrega.Observacoes
                             }).FirstOrDefault();

                if (itens != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, itens);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/entrega", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/obraEntrega")]
        public async Task<HttpResponseMessage> ObraEntrega([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/obraProjetos", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var itens = (from item in db.PessoaObraEntrega
                             where item.IdObra == m.Id && !item.Entregue
                             select new
                             {
                                 item.Id,
                                 item.DataAgendadaEntrega,
                                 item.DataConcluidaEntrega,
                                 item.Entregue,
                                 item.Observacoes,
                                 Checklist = (from itemCheck in db.PessoaObraEntregaChecklist
                                              where itemCheck.IdObraEntregue == item.Id
                                              select new
                                              {
                                                  itemCheck.Id,
                                                  itemCheck.Checklist.Nome,
                                                  itemCheck.OK
                                              }).ToList()
                             }).ToList(); //TODO - MUDAR AQUI

                if (itens != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, itens);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/obraProjetos", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/obraEntregaNova")]
        public async Task<HttpResponseMessage> ObraEntregaNova([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/obraProjetos", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                List<string> txt = new List<string>();
                var pe = db.PessoaObraEntrega.FirstOrDefault(x => x.Id == m.Id);
                if (pe != null)
                {
                    if (pe.Andamentos != null)
                    {
                        foreach (var item3 in pe.Andamentos)
                        {
                            var obra = db.PessoaObraAndamento.FirstOrDefault(x => x.Id == item3.IdObraAndamento);
                            if (obra != null)
                            {
                                if (obra.IndexPavimento > 0 && obra.ObraServico.Servico.Nome != null)
                                {
                                    txt.Add("Pavimento nº " + obra.IndexPavimento.ToString() + " / " + "Serviço " + obra.ObraServico.Servico.Nome);

                                }

                            }
                        }
                    }
                }
                
                var itens = (from item in db.PessoaObraEntrega
                             where item.Id == m.Id && !item.Entregue
                             select new
                             {
                                 item.Id,
                                 item.DataAgendadaEntrega,
                                 item.DataConcluidaEntrega,
                                 item.Entregue,
                                 item.Observacoes,
                                 Pavimentos = txt,
                                 Checklist = (from itemCheck in db.AreaChecklistItem
                                              join d in db.ChecklistItem on itemCheck.IdChecklistItem equals d.Id
                                              where itemCheck.IdPessoaObraEntrega == item.Id && !itemCheck.Removido && itemCheck.Tipo == AreaChecklistItemTipoEnum.Entregas
                                              select new
                                              {
                                                  itemCheck.Id,
                                                  d.Descricao,
                                                  itemCheck.DataInicio,
                                                  itemCheck.DataConclusao
                                              }).ToList()
                             }).ToList(); //TODO - MUDAR AQUI

                if (itens != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, itens);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/obraProjetos", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }
        [HttpPost]
        [Route("api/obraEntregaNovaPasso")]
        public async Task<HttpResponseMessage> ObraEntregaNovaPasso([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/obraProjetos", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                
                var check = db.AreaChecklistItem.FirstOrDefault(x => x.Id == m.Id && x.Tipo == AreaChecklistItemTipoEnum.Entregas);
                

                if (check != null)
                {
                    if (m.Tipo == 1)
                    {
                        check.DataConclusao = Helper.GetDateNow();
                        db.SaveChanges();
                    }
                    if (m.Tipo == 2)
                    {
                        check.DataConclusao = null;
                        db.SaveChanges();
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, check);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/obraProjetos", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/obraEntregaNovaConclui")]
        public async Task<HttpResponseMessage> ObraEntregaNovaConclui([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/obraProjetos", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }


                var check = db.PessoaObraEntrega.FirstOrDefault(x => x.Id == m.Id && !x.Entregue);


                if (check != null)
                {
                    check.Entregue = true;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, check);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/obraProjetos", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/obraVisitaNova")]
        public async Task<HttpResponseMessage> ObraVisitaNova([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/obraProjetos", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }
                
                var itens = (from item in db.PessoaObraVisita
                             where item.Id == m.Id && item.DataVisitaConcluida == null
                             select new
                             {
                                 item.Id,
                                 item.UsuarioAbertura,
                                 item.SolicitanteVisita,
                                 item.DataVisitaAgendada,
                                 item.MotivoVisita,
                                 item.DataVisitaConcluida,
                                 item.Observacoes,
                                 Checklist = (from itemCheck in db.AreaChecklistItem
                                              join d in db.ChecklistItem on itemCheck.IdChecklistItem equals d.Id
                                              where itemCheck.IdPessoaObraVisita == item.Id && !itemCheck.Removido && itemCheck.Tipo == AreaChecklistItemTipoEnum.Visitas
                                              select new
                                              {
                                                  itemCheck.Id,
                                                  d.Descricao,
                                                  itemCheck.DataInicio,
                                                  itemCheck.DataConclusao
                                              }).ToList()
                             }).ToList(); //TODO - MUDAR AQUI

                if (itens != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, itens);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/obraProjetos", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/obraVisitaConcluida")]
        public async Task<HttpResponseMessage> ObraVisitaConcluida([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/obraProjetos", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var itens = (from item in db.PessoaObraVisita
                             where item.IdObra == m.Id && item.DataVisitaConcluida != null
                             select new
                             {
                                 item.Id,
                                 item.UsuarioAbertura,
                                 item.SolicitanteVisita,
                                 item.DataVisitaAgendada,
                                 item.MotivoVisita,
                                 item.DataVisitaConcluida,
                                 item.Observacoes                                 
                              }).ToList(); //TODO - MUDAR AQUI

                if (itens != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, itens);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/obraProjetos", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/obraVisitaObs")]
        public async Task<HttpResponseMessage> ObraVisitaObs([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/obraProjetos", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var itens = db.PessoaObraVisita.FirstOrDefault(x => x.Id == m.Id);
                itens.Observacoes = m.Msg;
                db.SaveChanges();

                if (itens != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, itens);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/obraProjetos", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/obraObservacaoSalvar")]
        public async Task<HttpResponseMessage> obraObservacaoSalvar([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/obraProjetos", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var itens = db.AreaObservacao.FirstOrDefault(x => x.Id == m.Id);
                itens.Observacao = m.Msg;
                db.SaveChanges();

                if (itens != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, true);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/obraProjetos", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/obraObservacaoSalvarNovo")]
        public async Task<HttpResponseMessage> obraObservacaoSalvarNovo([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/obraProjetos", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.IdUsuario == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var observacaoDb = new AreaObservacao()
                {
                    DataCriacao = DateTime.Now,
                    Observacao = m.Msg,
                    IdArea = 100,
                    IdPessoaUsuario = m.IdUsuario,
                    IdPessoaObra = m.IdObra
                };

                db.AreaObservacao.Add(observacaoDb);
                db.SaveChanges();

                var itens = db.AreaObservacao.OrderByDescending(x => x.Id).FirstOrDefault();

                if (itens != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, true);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/obraProjetos", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/obraObservacao")]
        public async Task<HttpResponseMessage> ObraObservacao([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/obraProjetos", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.IdObra == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                //var res = new AreaTarefasDocumentosObservacoesDTO() { IdArea = 100, TipoArea = AreaTipoEnum.Processos };
                
                var res = (from o in db.AreaObservacao
                                   join p in db.PessoaUsuario on o.IdPessoaUsuario equals p.Id
                                   where !o.Removido && o.IdPessoaObra == m.IdObra
                                   select new 
                                   {
                                       DataCriacao = o.DataCriacao,
                                       Id = o.Id,
                                       IdArea = 100,
                                       IdPessoaUsuario = o.IdPessoaUsuario,
                                       NomePessoaUsuario = p.Nome,
                                       TextoObservacao = o.Observacao
                                   }).OrderByDescending(x => x.DataCriacao).ToList();

                if (res != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, res);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/obraProjetos", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }


        [HttpPost]
        [Route("api/obraVisitaNovaPasso")]
        public async Task<HttpResponseMessage> ObraVisitaNovaPasso([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/obraProjetos", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }


                var check = db.AreaChecklistItem.FirstOrDefault(x => x.Id == m.Id && x.Tipo == AreaChecklistItemTipoEnum.Visitas);


                if (check != null)
                {
                    if (m.Tipo == 1)
                    {
                        check.DataConclusao = Helper.GetDateNow();
                        db.SaveChanges();
                    }
                    if (m.Tipo == 2)
                    {
                        check.DataConclusao = null;
                        db.SaveChanges();
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, check);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/obraProjetos", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/obraVisitaNovaConclui")]
        public async Task<HttpResponseMessage> ObraVisitaNovaConclui([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/obraProjetos", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }


                var check = db.PessoaObraVisita.FirstOrDefault(x => x.Id == m.Id && x.DataVisitaConcluida == null);


                if (check != null)
                {
                    check.DataVisitaConcluida = Helper.GetDateNow();
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, check);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/obraProjetos", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/obraChecklist")]
        public async Task<HttpResponseMessage> ObraChecklist([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/obraChecklist", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0 || m.IdArea == 0 || m.IdUsuario == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var itens = (from item in db.PessoaObraTarefa
                             where item.IdPessoaObra == m.Id && item.Tarefa.IdArea == m.IdArea && item.IdPessoaUsuario == m.IdUsuario
                             select new
                             {
                                 item.Id,
                                 Nome = item.Tarefa.Descricao,
                                 OK = item.DataTermino.HasValue
                             }).ToList();

                if (itens.Count == 0)
                {
                    itens = (from item in db.PessoaObraTarefa
                             where item.IdPessoaObra == m.Id && item.Tarefa.IdArea == m.IdArea
                             select new
                             {
                                 item.Id,
                                 Nome = item.Tarefa.Descricao,
                                 OK = item.DataTermino.HasValue
                             }).ToList();
                }

                if (itens != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, itens);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/obraChecklist", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/alteraObraChecklist")]
        public async Task<HttpResponseMessage> AlteraObraChecklist([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                //deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/alteraObraChecklist");

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                using (var db = new DAO())
                {
                    if (m.ChecklistConclusao != null && m.ChecklistConclusao.Any())
                    {
                        foreach (var itemCheck in m.ChecklistConclusao)
                        {
                            var itemCk = db.PessoaObraTarefa.FirstOrDefault(f => f.Id == itemCheck.Id);
                            if (itemCk != null && !itemCk.DataTermino.HasValue && itemCheck.OK)
                            {
                                itemCk.DataInicio = Helper.GetDateNow();
                                itemCk.DataTermino = Helper.GetDateNow();
                                db.Entry(itemCk).State = EntityState.Modified;
                                db.SaveChanges();
                            }
                        }
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/alteraObraChecklist", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/entregas")]
        public async Task<HttpResponseMessage> Entregas([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/entregas", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                var itens = (from item in db.PessoaObraEntrega
                             where item.Id == m.Id
                             select new
                             {
                                 item.Id,
                                 item.DataAgendadaEntrega,
                                 item.DataConcluidaEntrega,
                                 item.Entregue,
                                 Checklist = (from itemCheck in db.PessoaObraEntregaChecklist
                                              where itemCheck.IdObraEntregue == item.Id
                                              select new
                                              {
                                                  itemCheck.Id,
                                                  itemCheck.Checklist.Nome,
                                                  itemCheck.OK
                                              }).ToList()
                             }).ToList();

                if (itens != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, itens);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/entregas", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/obraEntregaCria")]
        public async Task<HttpResponseMessage> ObraEntregaCria([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/obraProjetos", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }


                db.PessoaObraEntrega.Add(new PessoaObraEntrega(){ 
                    IdObra = m.Id,
                    DataAgendadaEntrega = m.DataTarefa,
                    Entregue = false,
                    Observacoes = "Criada no App",
                    IdUsuarioAbertura = m.IdUsuario,
                    IdUsuarioEntrega = m.IdUsuario,
                    ObservacaoInterna = ""
                });
                db.SaveChanges();

                var temp = db.PessoaObraEntrega.OrderByDescending(x => x.Id).FirstOrDefault(); // recupera Id recém inserido no banco

                //
                //Criar checklist específico para esta entrega -- dbo.AreaCkecklistItem
                //
                var _idArea_ = 146;
                var _idCkeckList_ = 1136;
                var _idPessoaObraEntrega_ = temp.Id;
                var _idPessoaUsuario_ = temp.IdUsuarioEntrega;

                var ck = db.ChecklistItem.Where(x => x.IdChecklist == _idCkeckList_);
                foreach (var ckItem in ck)
                {
                    var areaChecklistItem = new AreaChecklistItem()
                    {
                        IdArea = _idArea_,
                        IdChecklistItem = ckItem.Id,
                        IdPessoaUsuario = _idPessoaUsuario_,
                        DataCriacao = Helper.GetDateNow(),
                        DataInicio = null,
                        DataConclusao = null,
                        Removido = false,
                        IdPessoaObraEntrega = _idPessoaObraEntrega_,
                        Tipo = AreaChecklistItemTipoEnum.Entregas
                    };
                    db.AreaChecklistItem.Add(areaChecklistItem);

                }
                db.SaveChanges();
                //
                //Fim dbo.AreaCkecklistItem
                //
                var pessoaObraEntregaAndamento = new PessoaObraEntregaAndamento()
                {
                    IdObraEntregue = temp.Id,
                    IdObraAndamento = m.Qtde,
                    RomaneioOK = false
                };
                db.PessoaObraEntregaAndamento.Add(pessoaObraEntregaAndamento);
                db.SaveChanges();

                //var Obra = db.PessoaObraEntrega.Where(x => x.IdObra == pessoaObraEntrega.IdObra).ToList();

                db.Notificacao.Add(new Notificacao()
                {
                    Data = temp.DataAgendadaEntrega,
                    Mensagem = " Solicitacao de Entrega ",
                    IdUsuario = temp.IdUsuarioAbertura,
                    Lida = false,
                    IdInterno = temp.Id,
                    Tipo = NotificacaoTipoEnum.Entrega
                });
                db.SaveChanges();
                Extensions.SendNotificationOneSignal("Solicitação de entrega", "Solicitação de entrega", 0, temp.Id.ToString(), temp.Id.ToString(), temp.IdUsuarioAbertura.ToString());

                if (temp != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, true);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/obraProjetos", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("api/obraVisitaCria")]
        public async Task<HttpResponseMessage> ObraVisitaCria([FromBody] ApiAdminViewModel m)
        {
            var deviceInfos = "";
            try
            {
                var json = new JavaScriptSerializer().Serialize(m);
                deviceInfos = m.DeviceInfos;

                if (debug) new Logging().AddError(json, "Api/obraProjetos", infos: m.DeviceInfos);

                if (StringCipher.Decrypt(m.Token) != token)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }

                if (m.Id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não autorizado");
                }


                db.PessoaObraVisita.Add(new PessoaObraVisita()
                {
                    IdObra = m.Id,
                    DataVisitaAgendada = m.DataTarefa,
                    SolicitanteVisita = m.Detalhes,
                    MotivoVisita = m.Responsavel,
                    Observacoes = "Criada no APP",
                    IdUsuarioAbertura = m.IdUsuario
                });
                db.SaveChanges();

                var temp = db.PessoaObraVisita.OrderByDescending(x => x.Id).FirstOrDefault(); // recupera Id recém inserido no banco


                db.PessoaObraVisitaUsuario.Add(new PessoaObraVisitaUsuario() { 
                    IdObraVisita = temp.Id,
                    IdUsuario = temp.IdUsuarioAbertura
                });
                db.SaveChanges();
                //
                //Criar checklist específico para esta entrega -- dbo.AreaCkecklistItem
                //
                var _idArea_ = 101;
                var _idCkeckList_ = 1137;
                var _idPessoaObraVisita_ = temp.Id;
                var _idPessoaUsuario_ = m.IdUsuario;

               
                var ck = db.ChecklistItem.Where(x => x.IdChecklist == _idCkeckList_);
                foreach (var ckItem in ck)
                {
                    var areaChecklistItem = new AreaChecklistItem()
                    {
                        IdArea = _idArea_,
                        IdChecklistItem = ckItem.Id,
                        IdPessoaUsuario = _idPessoaUsuario_,
                        DataCriacao = Helper.GetDateNow(),
                        DataInicio = null,
                        DataConclusao = null,
                        Removido = false,
                        IdPessoaObraVisita = _idPessoaObraVisita_,
                        Tipo = AreaChecklistItemTipoEnum.Visitas
                    };
                    db.AreaChecklistItem.Add(areaChecklistItem);

                }
                db.SaveChanges();
                //
                //Fim dbo.AreaCkecklistItem
                //
                
                db.PessoaObraVisitaUsuario.Add(new PessoaObraVisitaUsuario()
                {
                    IdObraVisita = temp.Id,
                    IdUsuario = m.IdUsuarioCriacao
                });

                SOP.Backoffice.Helpers.Extensions.SendNotificationOneSignal("Solicitação de Visita", "Solicitação de Visita", 0, temp.Id.ToString(), temp.Id.ToString(), temp.ToString());

                db.SaveChanges();


                if (temp != null)
                {
                    
                    return Request.CreateResponse(HttpStatusCode.OK, true);
                }

                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Não Autorizado");
            }
            catch (Exception ex)
            {
                new Logging().AddError(ex, "Api/obraProjetos", infos: deviceInfos);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Erro: {ex.Message}");
            }
        }
    }

    public enum TipoArquivos
    {
        Visita = 0,
        ConclusaoObra = 1,
        EditarRequisicao = 2,
        Entrega = 3,
        Projeto = 4
    }
}