using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Exception;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.GroupUpdate;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;
using VkNet.Utils;

namespace BL
{
    public class Logical
    {
        VkApi api { get; set; } = new VkApi();
        string Token { get; set; } = "ed7ac37a9d6b777f62b3499858caaabadc7ddb75238e7e356cf0f4d6a2ea28b5d99c3831c912a1c21099e";
        string ConfigPath { get; set; } = Environment.CurrentDirectory + @"\Config";
        long? IdSender { get; set; }
        List<string> Commands { get; set; }
        List<string> Words { get; set; }
        List<string> AdminIds { get; set; }
        List<string> TempUsers { get; set; } = new List<string>();
        char Separate { get; set; } = '~';
        ulong GroupId { get; set; } = 195081922;
        string Server { get; set; }
        string Key { get; set; }
        string Ts { get; set; }
        public void Start()
        {
            api.Authorize(new ApiAuthParams()
            {
                Settings = Settings.All,
                AccessToken = Token,
                ApplicationId = 7452798,
            });
            LoadLibrary();
            LoadData();
            RefreshServer();
            CatchMessages();
        }
        private void RefreshServer()
        {
            var LPServer = api.Groups.GetLongPollServer(GroupId);
            Server = LPServer.Server;
            Key = LPServer.Key;
            Ts = LPServer.Ts;
        }
        public void CatchMessages()
        {
        Refresh:
            //try
            //{
            while (true)
            {
                var longPollHistory = api.Groups.GetBotsLongPollHistory(new BotsLongPollHistoryParams { Server = Server, Key = Key, Ts = Ts, Wait = 10 });
                if (longPollHistory.Updates.Any())
                {
                    foreach (var update in longPollHistory.Updates)
                    {
                        if (update?.Type == GroupUpdateType.MessageNew)
                        {
                            string message = update.Message.Body;
                            string mes = TreatMessage(message.ToLower().Trim());
                            IdSender = update.Message.UserId;
                            if (!CheckTemp())
                            {
                                if (mes != message.ToLower().Trim())
                                    SendMessage(mes);
                                else
                                    TreatCommand(mes.ToLower().Trim());
                            }
                            else
                            {
                                if (CheckAdmin(IdSender))
                                    AdminCommands(message);
                            }

                        }
                    }
                }
                Ts = longPollHistory.Ts;
            }
            //}
            //catch
            //{
            //    RefreshServer();
            //    goto Refresh;
            //}
        }
        public string TreatMessage(string message)
        {
            if (Words.Contains(message))
            {
                int index = Words.IndexOf(message);
                return Commands[index].Substring(Commands[index].IndexOf(Separate) + 1);
            }
            return message;
        }
        public void TreatCommand(string mes)
        {
            if (CheckAdmin(IdSender))
                switch (mes)
                {
                    case "добавить команду":
                        SendMessage("Введите команду в формате команда~текст");
                        TempUsers.Add($"{IdSender}|добавить команду");
                        break;
                    case "удалить команду":
                        SendMessage("Введите команду для удаления");
                        TempUsers.Add($"{IdSender}|удалить команду");
                        break;
                    case "добавить администратора":
                        SendMessage("Отправьте ID пользователя");
                        TempUsers.Add($"{IdSender}|добавить администратора");
                        break;
                    case "удалить администратора":
                        SendMessage("Отправьте ID администратора");
                        TempUsers.Add($"{IdSender}|удалить администратора");
                        break;
                    default:
                        SendKeyboard();
                        break;
                }
            else
                switch (mes)
                {
                    case "меню":
                        break;
                    default:
                        SendKeyboard();
                        break;

                }
        }
        bool CheckTemp()
        {
            if (TempUsers.Select(line => line.Substring(0, line.IndexOf('|'))).Contains(IdSender.ToString()))
                return true;
            return false;
        }
        void DeleteTempId()
        {
            int index = TempUsers.Select(line => line.Substring(0, line.IndexOf('|'))).ToList().IndexOf(IdSender.ToString());
            TempUsers.RemoveAt(index);
        }
        bool CheckAdmin(long? id)
        {
            if (AdminIds.Contains(id.ToString()))
                return true;
            return false;
        }
        public void AdminCommands(string command)
        {
            string Line = TempUsers.Where(line => line.Contains(IdSender.ToString())).ToList()[0];
            switch (Line.Split('|')[1])
            {
                case "добавить команду":
                    AddCommand(command);
                    break;
                case "удалить команду":
                    RemoveCommand(command.Trim().ToLower());
                    break;
                case "добавить администратора":
                    try
                    {
                        AddModder(command);
                    }
                    catch
                    {
                        SendMessage("Это не ID");
                    }
                    break;
                case "удалить администратора":
                    try
                    {
                        RemoveModder(command);
                    }
                    catch
                    {
                        SendMessage("Это не ID");
                    }
                    break;
                default:
                    SendKeyboard();
                    break;
            }
            DeleteTempId();
        }
        public void UserCommand(string command)
        {
            string Line = TempUsers.Where(line => line.Contains(IdSender.ToString())).ToList()[0];
            switch (Line.Split('|')[1])
            {
                case "команды":
                    MyCommands();
                    break;
                case "удалить команду":
                    RemoveCommand(command.Trim().ToLower());
                    break;
                case "меню":
                    DeleteTempId();
                    SendKeyboard();
                    break;
            }
        }
        public void SendMessage(string message)
        {
            if (message != null)
                api.Messages.Send(new MessagesSendParams
                {
                    UserId = IdSender,
                    Message = message,
                    RandomId = new Random().Next(999999)
                });
        }
        public void LoadLibrary()
        {
            if (!Directory.Exists(ConfigPath))
                Directory.CreateDirectory(ConfigPath);
            if (!File.Exists(ConfigPath + @"\Commands.txt"))
            {
                File.Create(ConfigPath + @"\Commands.txt");
            }
            if (!File.Exists(ConfigPath + @"\Admins.txt"))
            {
                File.Create(ConfigPath + @"\Admins.txt");
            }
            if (!File.Exists(ConfigPath + @"\Token.txt"))
            {
                File.Create(ConfigPath + @"\Token.txt");
            }
            if (!File.Exists(ConfigPath + @"\Log.txt"))
            {
                File.Create(ConfigPath + @"\Log.txt");
            }
            File.Create(ConfigPath + @"\Temp.txt");
        }
        public void LoadData()
        {
            Commands = File.ReadAllLines(ConfigPath + @"\Commands.txt").Where(Command => Command != "").Select(Command => Command.Trim()).ToList();
            Words = Commands.Select(command => command.Substring(0, command.IndexOf(Separate)).Trim().ToLower()).ToList();
            AdminIds = File.ReadAllLines(ConfigPath + @"\Admins.txt").Where(ID => ID != "").Select(ID => ID.Trim()).ToList();
        }
        public void RefreshData()
        {
            File.WriteAllLines(ConfigPath + @"\Commands.txt", Commands);
            File.WriteAllLines(ConfigPath + @"\Admins.txt", AdminIds);
        }
        public void AddCommand(string command)
        {
            if (command.Contains(Separate))
            {
                if (command.Substring(0, command.IndexOf(Separate)).Length > 0 & command.Substring(command.IndexOf(Separate)).Length > 0)
                    if (!Words.Contains(command.ToLower().Trim().Substring(0, command.IndexOf(Separate))))
                    {
                        Commands.Add(command);
                        Words.Add(command.Substring(0, command.IndexOf(Separate)).Trim().ToLower());
                        RefreshData();
                        SendMessage("Команда успешно добавлена!");
                        Logging($"добавил команду {command}.");
                    }
                    else
                        SendMessage("Команда есть в списке!");
            }
            else
                SendMessage("Неверный формат команды!");
        }
        public void RemoveCommand(string command)
        {
            if (Words.Contains(command))
            {
                int index = Words.IndexOf(command);
                Words.RemoveAt(index);
                Commands.RemoveAt(index);
                RefreshData();
                SendMessage("Команда успешно удалена!");
                Logging($"удалил команду {command}");
            }
        }
        public void AddModder(string id)
        {
            List<string> users = api.Groups.GetMembers(new GroupsGetMembersParams
            {
                GroupId = GroupId.ToString(),
            }).Select(x => x.Id.ToString())
              .ToList();
            List<string> admins = api.Groups.GetMembers(new GroupsGetMembersParams
            {
                GroupId = GroupId.ToString(),
                Filter = GroupsMemberFilters.Managers
            }).Select(x => x.Id.ToString()).ToList();
            if (!CheckAdmin(Convert.ToInt64(id)))
            {
                if (users.Contains(id.ToString()))
                    if (admins.Contains(id.ToString()))
                        try
                        {
                            AdminIds.Add(id);
                            RefreshData();
                            SendMessage("Администратор добавлен");
                            Logging($"id{IdSender} добавил администратора [{id}].");
                        }
                        catch (Exception e)
                        {
                            SendMessage("Произошла ошибка!");
                            Logging($"{e.Message} при попытке добавить администратора.");
                        }
                    else
                        SendMessage("ID не является руководителем группы!");
                else
                    SendMessage("ID не является участником группы!");
            }
            else
                SendMessage("Этот id уже в списке!");

        }
        public void RemoveModder(string id)
        {
            if (AdminIds.Contains(id))
            {
                AdminIds.Remove(id);
                RefreshData();
                SendMessage("Администратор успешно удален");
                Logging($"{IdSender} удалил администратора {id}.");
            }
            else
                SendMessage("Нет такого администратора!");
        }
        public void Logging(string Situation)
        {
            File.AppendAllText(ConfigPath + @"\Log.txt", $"[{DateTime.Now}]: id{IdSender} {Situation}" + Environment.NewLine);
        }
        public void MyCommands()
        {
            int Sheets = Commands.Count / 3;
            List<List<string>> CommandsList = new List<List<string>>();
            for(int i = 0; i < Sheets; i++)
            {
                CommandsList.Add(new List<string>());
            }
            foreach(string Command in Commands)
            {
                CommandsList[Commands.IndexOf(Command) / 3].Add(Command);
            }
        }
        void SendKeyboard()
        {
            switch (CheckAdmin(IdSender))
            {
                case true:
                    api.Messages.Send(new MessagesSendParams
                    {
                        UserId = IdSender,
                        Keyboard = new KeyboardBuilder()
                                    .AddButton("Добавить администратора", "btnValue", KeyboardButtonColor.Primary)
                                    .SetInline(false)
                                    .AddLine()
                                    .AddButton("Удалить администратора", "btnValue", KeyboardButtonColor.Primary)
                                    .SetInline(false)
                                    .AddLine()
                                    .AddButton("Добавить команду", "btnValue", KeyboardButtonColor.Primary)
                                    .SetInline(false)
                                    .AddLine()
                                    .AddButton("Удалить команду", "btnValue", KeyboardButtonColor.Primary)
                                    .SetInline(false)
                                    .Build(),
                        RandomId = new Random().Next(999999),
                        Message = "Для продолжения работы, выберите нужную команду!"
                    });
                    break;
                case false:
                    api.Messages.Send(new MessagesSendParams
                    {
                        UserId = IdSender,
                        Keyboard = new KeyboardBuilder()
                                    .AddButton("Обратная связь", "btnValue", KeyboardButtonColor.Primary)
                                    .SetInline(false)
                                    .SetOneTime()
                                    .AddLine()
                                    .AddButton("Команды", "btnValue", KeyboardButtonColor.Primary)
                                    .SetInline(false)
                                    .SetOneTime()
                                    .AddLine()
                                    .Build(),
                        RandomId = new Random().Next(999999),
                        Message = "Для того, чтобы продолжить общение, выбери нужную команду!"
                    });
                    break;
            }
        } //отправка клавиатуры
    }
}
