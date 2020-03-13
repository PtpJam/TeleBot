using System;
using Telegram.Bot;
using Telegram.Bot.Args;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;

namespace TeleBot
{
    class Program
    {

        static TelegramBotClient client;
        static string path = "botDB.sqlite";
        static void Main(string[] args)
        {
            if (!CheckExistDataBase_Async(path).GetAwaiter().GetResult())
                CreateDataBase_Async(path);

            client = new TelegramBotClient("1030848716:AAGfvtCqc0bL6HV9y_2ddXsRi96GkKnekD0");
            client.OnMessage += getMsg;
            client.StartReceiving();
            client.OnMessageEdited += editMsg;

            Console.Read();
        }

        private static void editMsg(object sender, MessageEventArgs e)
        {
            Console.WriteLine($"Msg {e.Message.Text} edited");
        }

        /// <summary>
        /// chech is exist by way
        /// </summary>
        /// <param name="path">Path to DataBase file</param>
        /// <returns></returns>
        private static bool CheckExistDataBase(string path) => File.Exists(path);
        private static async Task<bool> CheckExistDataBase_Async(string path) => await Task<bool>.Run(() => CheckExistDataBase(path));
        /// <summary>
        /// create empty database file by path
        /// </summary>
        /// <param name="path">Path to DataBase file</param>
        private static void CreateDataBase(string path)
        {
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source = {path}"))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Answer" +
                    "([id] INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "[text] VARCHAR(255) NOT NULL," +
                    "[ID_QUESTION] INTEGER," +
                    "FOREIGN KEY(ID_QUESTION) REFERENCES Question(ID));", connection))
                {
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                using (SQLiteCommand command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Question" +
                    "([id] INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "[text] VARCHAR(255) NOT NULL)", connection))
                {
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }
        private static async void CreateDataBase_Async(string path)  => await Task.Run(() => CreateDataBase(path));
        /// <summary>
        /// event for inner message in bot from user
        /// </summary>
        /// <param name="sender">Same Bisness logik entity</param>
        /// <param name="e">Params of inner msg</param>
        private static void getMsg(object sender, MessageEventArgs e)
        {
            if (e.Message.Text.Contains("/"))
                return;
            if (e.Message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
                return;

            if (e.Message.ReplyToMessage != null)
            {
                AddAnswerForQuestionInDataBase_Async(GetIdQuestionInDataBase_Async(e.Message.ReplyToMessage.Text, path).GetAwaiter().GetResult(), e.Message.Text, path);
            }

            if (!IsQuestionInDataBase_Async(e.Message.Text, path).GetAwaiter().GetResult())
            {
                AddQuestionInDataBase_Async(e.Message.Text, path);
                Console.WriteLine($"Добавлен вопрос {e.Message.Text}");
                client.SendTextMessageAsync(e.Message.Chat.Id, "Я не знаю ответа... Расскажи мне его, Cударь (нажми редактировать вопрос)");
            }
            else
            {
                Console.WriteLine($"Ответ на вопрос {e.Message.Text} есть");
                client.SendTextMessageAsync(e.Message.Chat.Id, 
                    GetAnswerInDataBase_Async(GetIdQuestionInDataBase_Async(e.Message.Text, path).GetAwaiter().GetResult()
                    , path).GetAwaiter().GetResult());
            }
        }
        /// <summary>
        /// Add question in local data base file
        /// </summary>
        /// <param name="question">from user</param>
        /// <param name="path_to_db"> of information</param>
        private static void AddQuestionInDataBase(string question, string path_to_db)
        {
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source = {path}"))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand($"INSERT INTO Question([text]) VALUES (@text)", connection))
                {
                    try
                    {
                        command.Parameters.Add(new SQLiteParameter("@text", question));
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }
        private static async void AddQuestionInDataBase_Async(string question, string path_to_db) => 
            await Task<string>.Run(() => AddQuestionInDataBase(question, path_to_db));
        private static string GetAnswerInDataBase(int id_question, string path_to_db)
        {
            string s = String.Empty;
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source = {path}"))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand($"SELECT [text] FROM Answer WHERE [ID_QUESTION] = @id_q", connection))
                {
                    try
                    {
                        command.Parameters.Add(new SQLiteParameter("@id_q", id_question));
                        SQLiteDataReader reader = command.ExecuteReader();
                        if (reader.HasRows)
                        {
                            reader.Read();
                            //connection.Close();
                            s = reader.GetString(0);

                        reader.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                }
            }
            return s;
        }
        private static async Task<string> GetAnswerInDataBase_Async(int id_question, string path_to_db) => 
            await Task<string>.Run(() => GetAnswerInDataBase(id_question, path_to_db));
        private static int GetIdQuestionInDataBase(string question, string path_to_db)
        {
            int s = -1;
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source = {path}"))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand($"SELECT ID FROM Question WHERE [text] = @text", connection))
                {
                    try
                    {
                        command.Parameters.Add(new SQLiteParameter("@text", question));
                        SQLiteDataReader reader = command.ExecuteReader();
                        if (reader.HasRows)
                        {
                            reader.Read();
                            //connection.Close();
                            s = reader.GetInt32(0);
                            reader.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            return s;
        }
        private static async Task<int> GetIdQuestionInDataBase_Async(string question, string path_to_db) => 
            await Task<int>.Run(() => GetIdQuestionInDataBase(question, path_to_db));
        private static void AddAnswerForQuestionInDataBase(int id_question, string answer, string path_to_db)
        {
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source = {path}"))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand($"INSERT INTO Answer([text], [ID_QUESTION]) VALUES (@text, @id_q)", connection))
                {
                    try
                    {
                        command.Parameters.Add(new SQLiteParameter("@text", answer));
                        command.Parameters.Add(new SQLiteParameter("@id_q", id_question));
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }
        private static async void AddAnswerForQuestionInDataBase_Async(int id_question, string answer, string path_to_db) => 
            await Task.Run(() => AddAnswerForQuestionInDataBase(id_question, answer, path_to_db));
        /// <summary>
        /// Chech question in local data base file
        /// </summary>
        /// <param name="question">from user</param>
        /// <param name="path_to_db"> of information</param>
        /// <returns></returns>
        private static bool IsQuestionInDataBase(string question, string path_to_db)
        {
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source = {path}"))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand($"SELECT COUNT(*) FROM Question WHERE [TEXT] = @text", connection))
                {
                    try
                    {
                        command.Parameters.Add(new SQLiteParameter("@text", question));

                        object o = command.ExecuteScalar();
                        if (o != null)
                        {
                            int count = int.Parse(o.ToString());
                            Console.WriteLine(count);
                            return count > 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            return false;
        }
        private static async Task<bool> IsQuestionInDataBase_Async(string question, string path_to_db) => 
            await Task<bool>.Run(() => IsQuestionInDataBase(question, path_to_db));
    }
}
