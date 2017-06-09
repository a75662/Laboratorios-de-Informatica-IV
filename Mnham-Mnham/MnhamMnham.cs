using Android.Locations;
using System;
using System.Collections.Generic;

namespace Mnham_Mnham
{
    public class MnhamMnham
    {
        private int clienteAutenticado;
        private bool utilizadorEProprietario;

        public MnhamMnham()
        {
            estabelecimentos = new EstabelecimentoDAO();
            clientes = new ClienteDAO();
            pedidos = new PedidoDAO();
            proprietarios = new ProprietarioDAO();
        }

        // REGISTO E AUTENTICACAO

        public int IniciarSessao(string email, string palavraPasse)
        {
            Cliente cliente = clientes.ObterPorEmail(email);

            if (cliente != null)
            {
                Console.WriteLine("1. " + palavraPasse.Length + " 2. " + cliente.PalavraPasse.Length);
                if (palavraPasse.Equals(cliente.PalavraPasse, StringComparison.Ordinal))
                {
                    this.clienteAutenticado = cliente.Id;
                    this.utilizadorEProprietario = false;

                    return 1;
                }
            }
            /*Proprietario proprietario = proprietarios.ObterPorEmail(email);
            if (proprietario != null)
            {
                if (palavraPasse.Equals(proprietario.PalavraPasse))
                {
                    this.clienteAutenticado = proprietario.Id;
                    this.utilizadorEProprietario = true;

                    return 2;
                }
            }*/
            return 0;
        }

        // Clientes

        public bool IniciarSessaoCliente(string email, string palavraPasse)
        {
            Cliente cliente = clientes.ObterPorEmail(email);
            if (cliente != null)
            {
                if (palavraPasse.Equals(cliente.PalavraPasse))
                {
                    this.clienteAutenticado = cliente.Id;
                    this.utilizadorEProprietario = false;

                    return true;
                }
            }
            return false;
        }

        public bool RegistarCliente(Cliente cliente)
        {
            //retorna falso se email já existe
            return clientes.AdicionarCliente(cliente);
        }

        // Proprietarios

        public bool IniciarSessaoProprietario(string email, string palavraPasse)
        {
            Proprietario proprietario = proprietarios.ObterPorEmail(email);
            if (proprietario != null)
            {
                if (palavraPasse.Equals(proprietario.PalavraPasse))
                {
                    this.clienteAutenticado = proprietario.Id;
                    this.utilizadorEProprietario = true;

                    return true;
                }
            }
            return false;
        }

        public bool RegistarProprietario(Proprietario proprietario)
        {
            //retorna falso se email já existe
            return proprietarios.AdicionarProprietario(proprietario);
        }

        // GESTAO DE CONTA

        public void TerminarSessao()
        {
            this.clienteAutenticado = 0;
        }

        // Clientes

        public void EditarDados(Cliente cliente)
        {
            cliente.DefinirId(this.clienteAutenticado);
            clientes.EditarDados(cliente);
        }

        public Cliente ConsultarDadosCliente()
        {
            return clientes.ObterPorId(clienteAutenticado);
        }

        // Proprietarios

        public void EditarDados(Proprietario proprietario)
        {
            proprietario.DefinirId(this.clienteAutenticado);
            proprietarios.EditarDados(proprietario);
        }
        
        public Proprietario ConsultarDadosProprietario()
        {
            return proprietarios.ObterPorId(clienteAutenticado);
        }

        // PREFERENCIAS E NAO PREFERENCIAS

        public void RegistarPreferencia(ref Preferencia pref)
        {
            clientes.AdicionarPreferencia(clienteAutenticado, pref);
        }

        public void RegistarNaoPreferencia(ref Preferencia pref)
        {
            clientes.AdicionarNaoPreferencia(clienteAutenticado, pref);
        }

        public IList<Preferencia> ConsultarPreferencias()
        {
            return clientes.ConsultarPreferencias(clienteAutenticado);
        }

        public IList<Preferencia> ConsultarNaoPreferencias()
        {
            return clientes.ConsultarNaoPreferencias(clienteAutenticado);
        }

        public void RemoverPreferencia(ref string designacaoPreferencia, ref string designacaoAlimento)
        {
            Preferencia preferencia = new Preferencia(designacaoPreferencia, designacaoAlimento);
            clientes.RemoverPreferencia(clienteAutenticado, preferencia);
        }

        public void RemoverNaoPreferencia(ref string designacaoNaoPreferencia, ref string designacaoAlimento)
        {
            Preferencia naoPreferencia = new Preferencia(designacaoNaoPreferencia, designacaoAlimento);
            clientes.RemoverNaoPreferencia(clienteAutenticado, naoPreferencia);
        }

        // PEDIDOS E CONSULTAS

        public List<AlimentoEstabelecimento> EfetuarPedido(ref string termo, Location localizacao)
        {
            RegistaPedidoHistorico(ref termo);
            PedidoProcessado pedidoProcessado = new PedidoProcessado(termo);
            Cliente cliente;

            List<string> preferencias;
            List<string> naoPreferencias;

            if (clienteAutenticado != 0)
            {
                // cliente
                cliente = clientes.ObterPorId(clienteAutenticado);
                preferencias = cliente.ObterPreferencias(pedidoProcessado.ObterNomeAlimento());
                List<string> preferenciasPedido = pedidoProcessado.ObterPreferencias();
                preferencias.AddRange(preferenciasPedido);

                naoPreferencias = cliente.ObterNaoPreferencias(pedidoProcessado.ObterNomeAlimento());
                List<string> naoPreferenciasPedido = pedidoProcessado.ObterNaoPreferencias();
                naoPreferencias.AddRange(naoPreferenciasPedido);
            }
            else
            {
                // utilizador não autenticado
                preferencias = pedidoProcessado.ObterPreferencias();
                naoPreferencias = pedidoProcessado.ObterNaoPreferencias();
            }

            List<AlimentoEstabelecimento> listaAEs = new List<AlimentoEstabelecimento>();
            Dictionary<int, Estabelecimento> estabsObtidos = new Dictionary<int, Estabelecimento>();
            foreach (Alimento a in estabelecimentos.ObterAlimentos(pedidoProcessado.NomeAlimento))
            {
                if(a.ContemNaoPreferencias(naoPreferencias) == false)
                {
                    int nPreferencias = a.QuantasPreferenciasContem(preferencias);
                    Alimento alim = estabelecimentos.ObterAlimento(a.Id);
                    int idEstab = alim.IdEstabelecimento;
                    Estabelecimento estab;
                    if (estabsObtidos.TryGetValue(idEstab, out estab) == false)
                    {
                        estab = estabelecimentos.ObterEstabelecimento(idEstab);
                        estabsObtidos.Add(idEstab, estab);
                    }
                    float distancia = localizacao.DistanceTo(estab.Coords);
                    AlimentoEstabelecimento ae = new AlimentoEstabelecimento(nPreferencias, distancia, estab, alim);
                    listaAEs.Add(ae);
                }
            }

            listaAEs.Sort();

            return listaAEs;
        }

        private void RegistaPedidoHistorico(ref string termo)
        {
            if (clienteAutenticado != 0)
            {
                //cliente
                Pedido pedido = new Pedido(termo, clienteAutenticado);
                pedidos.AdicionarPedido(pedido);
            }
            else
            {
                // utilizador não autenticado
                // Como guardar??
            }
        }

        public AlimentoEstabelecimento ConsultarAlimento(ref int idAlimento)
        {
            Alimento a =  estabelecimentos.ObterAlimento(idAlimento);
            Estabelecimento e = estabelecimentos.ObterEstabelecimento(a.IdEstabelecimento);
            return new AlimentoEstabelecimento(e, a);
        }

        public Estabelecimento ConsultarEstabelecimento(ref int idEstabelecimento)
        {
            return estabelecimentos.ObterEstabelecimento(idEstabelecimento);
        }

        public IList<Pedido> ConsultarHistorico()
        {
            return pedidos.ObterPedidos(clienteAutenticado);
        }

        public SortedSet<Tendencia> ObterTendencias()
        {
            Dictionary<string, Tendencia> aux = new Dictionary<string, Tendencia>();
            foreach (string s in pedidos.ObterPedidosUltimaSemana())
            {
                Tendencia t;
                if (aux.TryGetValue(s, out t))
                {
                    t.inc();
                }
                else
                {
                    t = new Tendencia(s);
                    aux.Add(s, t);
                }
            }
            SortedSet<Tendencia> tendencias = new SortedSet<Tendencia>();
            foreach (Tendencia t in aux.Values)
            {
                tendencias.Add(t);
            }
            //Alterar para retornar as 5 mais frequentes!!
            return tendencias;
        }

        // CLASSIFICACOES E PARTILHA

        public void ClassificarAlimento(ref int idAlimento, ref int classificacao)
        {
            Classificacao cla = new Classificacao(classificacao, clienteAutenticado);
            estabelecimentos.ClassificarAlimento(idAlimento, cla);
        }

        public void ClassificarAlimento(ref int idAlimento, ref int classificacao, ref string comentario)
        {
            Classificacao cla = new Classificacao(classificacao, comentario, clienteAutenticado);
            estabelecimentos.ClassificarAlimento(idAlimento, cla);
        }

        public void ClassificarEstabelecimento(ref int idEstabelecimento, ref int classificacao)
        {
            Classificacao cla = new Classificacao(classificacao, clienteAutenticado);
            estabelecimentos.ClassificarEstabelecimento(idEstabelecimento, cla);
        }

        public void ClassificarEstabelecimento(ref int idEstabelecimento, ref int classificacao, ref string comentario)
        {
            Classificacao cla = new Classificacao(classificacao, comentario, clienteAutenticado);
            estabelecimentos.ClassificarEstabelecimento(idEstabelecimento, cla);
        }

        public IList<Classificacao> ConsultarClassificacoesAlimentos()
        {
            return estabelecimentos.ConsultarClassificacoesAlimentos(clienteAutenticado);
        }

        public IList<Classificacao> ConsultarClassificacoesEstabelecimentos()
        {
            return estabelecimentos.ConsultarClassificacoesEstabelecimentos(clienteAutenticado);
        }

        public void RemoverClassificacaoAlimento(ref int idAlimento)
        {
            estabelecimentos.RemoverClassificacaoAlimento(idAlimento, clienteAutenticado);
        }

        public void RemoverClassificacaoEstabelecimento(ref int idEstabelecimento)
        {
            estabelecimentos.RemoverClassificacaoAlimento(idEstabelecimento, clienteAutenticado);
        }

        // GESTAO DE ESTABELECIMENTO

        public void RegistarAlimento(ref int idEstabelecimento, Alimento alim)
        {
            proprietarios.RegistarAlimento(idEstabelecimento, alim);
        }

        public void EditarFotoAlimento(ref int idAlimento, ref byte[] foto)
        {
            proprietarios.EditarFotoAlimento(idAlimento, foto);
        }

        public void AdicionarIngredientesAlimento(ref int idAlimento, ref List<string> designacaoIngredientes)
        {
            proprietarios.AdicionarIngredientesAlimento(idAlimento, designacaoIngredientes);
        }

        public void RemoverIngredientesAlimento(ref int idAlimento, ref List<string> designacaoIngredientes)
        {
            proprietarios.RemoverIngredientesAlimento(idAlimento, designacaoIngredientes);
        }
        
        public void RemoverAlimento(ref int idAlimento)
        {
            proprietarios.RemoverAlimento(idAlimento);
        }
        
        public IList<Estabelecimento> ConsultarEstabelecimentos()
        {
            return proprietarios.ConsultarEstabelecimentos(clienteAutenticado);
        }

        public List<Alimento> ConsultarAlimentos(int idEstabelecimento)
        {
            return proprietarios.ConsultarAlimentos(idEstabelecimento);
        }

        private ProprietarioDAO proprietarios;
        private ClienteDAO clientes;
        private EstabelecimentoDAO estabelecimentos;
        private PedidoDAO pedidos;


        // FILTROS

        public List<AlimentoEstabelecimento> OrdenarCusto (List<AlimentoEstabelecimento> listaAEs, bool decrescente)
        {
            listaAEs.Sort(new ComparadorCusto());
            if (decrescente)
            {
                listaAEs.Reverse();
            }
            return listaAEs;
        }

        public List<AlimentoEstabelecimento> OrdenarDistancia (List<AlimentoEstabelecimento> listaAEs, bool decrescente)
        {
            listaAEs.Sort(new ComparadorDistancia());
            if (decrescente)
            {
                listaAEs.Reverse();
            }
            return listaAEs;
        }

        public List<AlimentoEstabelecimento> OrdenarClassificacaoAlimento (List<AlimentoEstabelecimento> listaAEs, bool crescente)
        {
            listaAEs.Sort(new ComparadorClassificaoAlimento());
            if (crescente)
            {
                listaAEs.Reverse();
            }
            return listaAEs;
        }

        public List<AlimentoEstabelecimento> OrdenarClassificacaoEstabelecimento(List<AlimentoEstabelecimento> listaAEs, bool crescente)
        {
            listaAEs.Sort(new ComparadorClassificaoEstabelecimento());
            if (crescente)
            {
                listaAEs.Reverse();
            }
            return listaAEs;
        }

        public List<AlimentoEstabelecimento> OrdenarOmissao (List<AlimentoEstabelecimento> listaAEs)
        {
            listaAEs.Sort();
            return listaAEs;
        }

        public List<AlimentoEstabelecimento> FiltrarPreco (List<AlimentoEstabelecimento> listaAEs, float preco)
        {
            List<AlimentoEstabelecimento> listaFiltrada = new List<AlimentoEstabelecimento>();
            foreach(AlimentoEstabelecimento ae in listaAEs)
            {
                if(ae.Alimento.Preco <= preco)
                {
                    listaFiltrada.Add(ae);
                }
            }
            return listaFiltrada;
        }

        public List<AlimentoEstabelecimento> FiltrarDistancia (List<AlimentoEstabelecimento> listaAEs, float distancia)
        {
            List<AlimentoEstabelecimento> listaFiltrada = new List<AlimentoEstabelecimento>();
            foreach (AlimentoEstabelecimento ae in listaAEs)
            {
                //if(ae.Distancia <= distancia)
                {
                    listaFiltrada.Add(ae);
                }
            }
            return listaFiltrada;
        }

        public List<AlimentoEstabelecimento> FiltrarClassificacaoAlimento (List<AlimentoEstabelecimento> listaAEs, int classificacao)
        {
            List<AlimentoEstabelecimento> listaFiltrada = new List<AlimentoEstabelecimento>();
            foreach (AlimentoEstabelecimento ae in listaAEs)
            {
                if(ae.Alimento.ClassificacaoMedia >= classificacao)
                {
                    listaFiltrada.Add(ae);
                }
            }
            return listaFiltrada;
        }

        public List<AlimentoEstabelecimento> FiltrarClassificacaoEstabelecimento (List<AlimentoEstabelecimento> listaAEs, int classificacao)
        {
            List<AlimentoEstabelecimento> listaFiltrada = new List<AlimentoEstabelecimento>();
            foreach (AlimentoEstabelecimento ae in listaAEs)
            {
                if (ae.Estabelecimento.ClassificacaoMedia >= classificacao)
                {
                    listaFiltrada.Add(ae);
                }
            }
            return listaFiltrada;
        }
    }

    internal class ComparadorClassificaoEstabelecimento : IComparer<AlimentoEstabelecimento>
    {
        public int Compare(AlimentoEstabelecimento x, AlimentoEstabelecimento y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                if (y == null)
                {
                    return 1;
                }
                else
                {
                    // Ordenar decrescentemente
                    return y.Estabelecimento.ClassificacaoMedia.CompareTo(x.Estabelecimento.ClassificacaoMedia);
                }
            }
        }
    }

    internal class ComparadorClassificaoAlimento : IComparer<AlimentoEstabelecimento>
    {
        public int Compare(AlimentoEstabelecimento x, AlimentoEstabelecimento y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                if (y == null)
                {
                    return 1;
                }
                else
                {
                    // Ordenar decrescentemente
                    return y.Alimento.ClassificacaoMedia.CompareTo(x.Alimento.ClassificacaoMedia);
                }
            }
        }
    }

    internal class ComparadorDistancia : IComparer<AlimentoEstabelecimento>
    {
        public int Compare(AlimentoEstabelecimento x, AlimentoEstabelecimento y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                if (y == null)
                {
                    return 1;
                }
                else
                {
                    // Ordenar crescentemente
                    return x.Distancia.CompareTo(y.Distancia);
                }
            }
        }
    }

    internal class ComparadorCusto : IComparer<AlimentoEstabelecimento>
    {
        public int Compare(AlimentoEstabelecimento x, AlimentoEstabelecimento y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                if (y == null)
                {
                    return 1;
                }
                else
                {
                    // Ordenar crescentemente
                    return Nullable.Compare<float>(x.Alimento.Preco, y.Alimento.Preco);
                }
            }
        }
    }
}