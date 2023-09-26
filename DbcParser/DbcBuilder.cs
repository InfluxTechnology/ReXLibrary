using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using DbcParser.Parsers;
using DbcParserLib.Model;

namespace DbcParserLib
{
    public class DbcBuilder : IDbcBuilder
    {
        private readonly ISet<Node> m_nodes = new HashSet<Node>(new NodeEqualityComparer());
        private readonly IDictionary<string, string> m_namedTables = new Dictionary<string, string>();
        private readonly IDictionary<uint, Message> m_messages = new Dictionary<uint, Message>();
        private readonly IDictionary<uint, IDictionary<string, Signal>> m_signals = new Dictionary<uint, IDictionary<string, Signal>>();
        // Influx Technology LTD
        private readonly IDictionary<string, DBCAttribute> m_dbcattributes = new Dictionary<string, DBCAttribute>();
        private readonly IDictionary<string, EnvVariable> m_envvariables = new Dictionary<string, EnvVariable>();
        public NameValueCollection AttrValues = new NameValueCollection();

        private Message m_currentMessage;
        private Attribute m_currentAttribute;

        public void SetDefaultAttrValues()
        {

            foreach (uint ID in AttributeDefaultParser.ID_List)
            {
                foreach (KeyValuePair<string, string> attrtypes in AttributeDefaultParser.AttrTypes)
                {

                    if (attrtypes.Key.Equals("VFrameFormat"))
                    {
                        if (m_messages[ID].AttrValues["VFrameFormat"] == null)
                        {

                            switch (attrtypes.Value)
                            {
                                case "StandardCAN": m_messages[ID].Type = Message.MsgType.Standard; break;
                                case "ExtendedCAN": m_messages[ID].Type = Message.MsgType.Extended; break;
                                case "CanFDStandard": m_messages[ID].Type = Message.MsgType.CanFDStandard; break;
                                case "CanFDExtended": m_messages[ID].Type = Message.MsgType.CanFDExtended; break;
                                case "J1939PG": m_messages[ID].Type = Message.MsgType.J1939PG; m_messages[ID].AttrValues["VFrameFormat"] = "3"; break;
                                case "Lin": m_messages[ID].Type = Message.MsgType.Lin; break;
                                case "reserved": m_messages[ID].Type = Message.MsgType.reserved; break;
                                default: throw new Exception("VFrameFormat Not Recognized");
                            }

                        }
                        else
                        {
                            switch (m_messages[ID].AttrValues["VFrameFormat"])
                            {
                                case "0": m_messages[ID].Type = Message.MsgType.Standard; break;
                                case "1": m_messages[ID].Type = Message.MsgType.Extended; break;
                                case "14": m_messages[ID].Type = Message.MsgType.CanFDStandard; break;
                                case "15": m_messages[ID].Type = Message.MsgType.CanFDExtended; break;
                                case "3": m_messages[ID].Type = Message.MsgType.J1939PG; break;
                                case "Lin": m_messages[ID].Type = Message.MsgType.Lin; break;


                            }
                        }
                    }
                    else if (attrtypes.Key.Equals("CANFD_BRS"))
                    {
                        if (m_messages[ID].AttrValues["CANFD_BRS"] == null)
                        {

                            m_messages[ID].AttrValues["CANFD_BRS"] = attrtypes.Value.ToString();
                            if (attrtypes.Value == "1")
                                m_messages[ID].BRS = true;
                            else
                                m_messages[ID].BRS = false;
                        }
                        else
                        {
                            switch(attrtypes.Value)
                            {
                                case "1": m_messages[ID].BRS = true; m_messages[ID].AttrValues["CABFD_BRS"] = "1";break;
                                case "0": m_messages[ID].BRS = false; m_messages[ID].AttrValues["CABFD_BRS"] = "0";break;
                            }
                        }
                    }

                }
            }
        }

            /* Checking for J1939PG 
            foreach (uint ID in Parser.ID_List)
            {
                if (m_messages[ID].AttrValues["VFrameFormat"] == null)
                {


             
                    if(Parser.isJ1939)
                    {
                        m_messages[ID].AttrValues["VFrameFormat"] = "3";
                        m_messages[ID].Type = Message.MsgType.J1939PG;
                    }

                }
                else
                {
                    if (m_messages[ID].AttrValues["VFrameFormat"] == "3")
                        m_messages[ID].Type = Message.MsgType.J1939PG;
                    else if (m_messages[ID].AttrValues["VFrameFormat"] == "14")
                        m_messages[ID].Type = Message.MsgType.reserved;
                }
            }
            */



        
        public void AddNode(Node node)
        {
            m_nodes.Add(node);
        }

        public void AddMessage(Message message)
        {
            m_messages[message.ID] = message;
            m_currentMessage = message;
            m_signals[message.ID] = new Dictionary<string, Signal>();
        }

        public void AddSignal(Signal signal)
        {
            if (m_currentMessage != null)
            {
                signal.ID = m_currentMessage.ID;
                m_signals[m_currentMessage.ID][signal.Name] = signal;
            }
        }

        public void AddSignalComment(uint messageID, string signalName, string comment)
        {
            if (TryGetValueMessageSignal(messageID, signalName, out var signal))
            {
                signal.Comment = comment;
            }
        }

 /*       public void AddNodeComment(string nodeName, string comment)
        {
            var node = m_nodes.FirstOrDefault(n => n.Name.Equals(nodeName));
            if (node != null)
            {
                node.Comment = comment;
            }
        }*/

        public void AddMessageComment(uint messageID, string comment)
        {
            if (m_messages.TryGetValue(messageID, out var message))
            {
                message.Comment = comment;
            }
        }

        public void AddNamedValueTable(string name, string values)
        {
            m_namedTables[name] = values;
        }

        public void AddDBCAttribute(DBCAttribute attribute)
        {
            m_dbcattributes[attribute.Name] = attribute;
        }

        public void AddEnvVariable(EnvVariable variable)
        {
            m_envvariables[variable.Name] = variable;
        }

        public DBCAttribute GetDBCAttribute(string name)
        {
            if (m_dbcattributes.TryGetValue(name, out var attribute))
                return attribute;
            
            return null;
        }

        public void AddAttribute(string name, string value)
        {
            AttrValues.Add(name, value);
        }

        public Node GetNode(string name)
        {
            var node = m_nodes.FirstOrDefault(n => n.Name.Equals(name));
            return node;
        }
        public Message GetMessage(uint ID)
        {
            if (m_messages.TryGetValue(ID, out var message))
                return message;

            return null;
        }

        public Signal GetSignal(uint ID, string name)
        {
            if(TryGetValueMessageSignal(ID, name, out var signal))
                return signal;

            return null;
        }

        public EnvVariable GetEnvVariable(string name)
        {
            if (m_envvariables.TryGetValue(name, out var envVariable))
                return envVariable;

            return null;
        }

        public void LinkTableValuesToSignal(uint messageId, string signalName, string values)
        {
            if (TryGetValueMessageSignal(messageId, signalName, out var signal))
            {
                signal.ValueTable = values;
            }
        }

        private bool TryGetValueMessageSignal(uint messageId, string signalName, out Signal signal)
        {
            if (m_signals.TryGetValue(messageId, out var signals) && signals.TryGetValue(signalName, out signal))
            {
                return true;
            }

            signal = null;
            return false;
        }

        public void LinkNamedTableToSignal(uint messageId, string signalName, string tableName)
        {
            if (m_namedTables.TryGetValue(tableName, out var values))
            {
                LinkTableValuesToSignal(messageId, signalName, values);
            }
        }

        public Dbc Build()
        {
            foreach (var message in m_messages)
            {
                message.Value.Signals.Clear();
                message.Value.Signals.AddRange(m_signals[message.Key].Values);
            }

            return new Dbc(m_nodes.ToArray(), m_messages.Values.ToArray());
        }
    }

    internal class NodeEqualityComparer : IEqualityComparer<Node>
    {
        public bool Equals(Node b1, Node b2)
        {
            if (b2 == null && b1 == null)
                return true;
            else if (b1 == null || b2 == null)
                return false;
            else if(b1.Name == b2.Name)
                return true;
            else
                return false;
        }

        public int GetHashCode(Node bx)
        {
            return bx.Name.GetHashCode();
        }
    }
}