using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using MySql.Data;
using MySql.Data.MySqlClient;
using MimeKit;
using MailKit.Net.Smtp;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text.Encodings.Web;
using System.IO;
using ProgramHanTwo.Models;
using System.Security.Principal;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Identity;
using static System.Formats.Asn1.AsnWriter;
using System.Drawing;
using MySqlX.XDevAPI.Common;

namespace Program_Han.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private static string connect = "Server=YourServer;Database=YourDB;Uid=YourName;Pwd=YourPassword"
;
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        private static MySqlConnection con = new MySqlConnection(connect);
        private static MySqlCommand command;
        private static MySqlDataReader reader;
        private static List<Person> listComment = new List<Person>();
        public async Task<IActionResult> Index(string? id1)
        {
            try
            {
                if(HttpContext.User.Identity.IsAuthenticated == true){
                    var identty = new ClaimsIdentity(User.Identity);
                    identty.RemoveClaim(identty.FindFirst("Score"));
                    identty.RemoveClaim(identty.FindFirst("Quantity"));
                    identty.RemoveClaim(identty.FindFirst("Level"));
                    identty.AddClaim(new Claim("Score", "0"));
                    identty.AddClaim(new Claim("Quantity", "4"));
                    identty.AddClaim(new Claim("Level", "1"));
                    var claimPrincipal = new ClaimsPrincipal(identty);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimPrincipal);
                }
                ViewData["Message"] = TempData["Message"];
                listComment.Clear();
                await con.OpenAsync();
                if (HttpContext.User.Identity.IsAuthenticated && id1 != null)
                {
                    command = new MySqlCommand($"INSERT INTO Comment(UserName,Photo,Article,ViewPage,Evaluate) values ('{User.FindFirst(ClaimTypes.Name)?.Value + "|" + User.FindFirst(ClaimTypes.Email)?.Value.Split('@')[0]}','{User.FindFirst("Photo")?.Value}','{id1}',\'Main\',{Program.UseModelWithSingleItem(Program.mlContext, Program.model, id1)})", con);
                    command.ExecuteNonQuery();
                }

                await ReturnComment("Main");
            }
            finally { await con.CloseAsync(); }
            return View(listComment);
        }
        //Тут идёт добавление в массив новых люъектов для отображения комментариев пользователей
        public static async Task ReturnComment(string ViewPage)
        {
            command = new MySqlCommand($"SELECT UserName,Photo,Article,ViewPage,Evaluate FROM Comment where ViewPage=\'{ViewPage}\'", con);
            reader = command.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                    listComment.Add(new Person { Name = reader.GetValue(0).ToString().Split('|')[0], Photo = reader.GetValue(1).ToString(), Article = reader.GetValue(2).ToString(), Evaluate= reader.GetByte(4) }); 
            }
            
        }
        public IActionResult Registration()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registration([Bind("Lastname", "Name", "Email")] Person pers)
        {
            try
            {
                await con.OpenAsync();
                if (pers.Email != null && pers.Lastname != null && pers.Name != null && pers.Name.Length < 50 && pers.Lastname.Length < 100)
                {
                    command = new MySqlCommand($"SELECT COUNT(*) AS Yet FROM Users where Email='{pers.Email}'", con); //Потом не забудь указать в ТЗ, что защита поставлена от всего.
                    int counter = Convert.ToInt16(command.ExecuteScalar());
                    if (counter == 0)
                    {
                        string password = Guid.NewGuid().ToString();
                        command = new MySqlCommand($"INSERT INTO Users(Email,Lastname,Name,Password,Photo) VALUES ('{pers.Email}','{pers.Lastname}','{pers.Name}','{password}','/img/DefaultPhoto.jpg');", con); //тут подправил
                        command.ExecuteNonQuery();

                        var emailMessage = new MimeMessage();

                        emailMessage.From.Add(new MailboxAddress("Администрация сайта", "YourEmailLogin"));
                        emailMessage.To.Add(new MailboxAddress("", pers.Email));
                        emailMessage.Subject = $"Здравствуй {pers.Name}.Рад видеть тебя на сайте Program-Han";
                        emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                        {
                            Text = $"Это сообщение вам пришло, потому что вы зарегестрировались на сайте Program-Han.\n\r Хозяин очень рад, что вы проявили внимание к его продукту. Возможно, он даже вас найдёт😁.\n\r Ваш логин: {pers.Email}\n\rПароль:{password}. В дальнейшем пароль можно поменять в лично кабинете."
                        };
                        using (var client = new SmtpClient())
                        {
                            await client.ConnectAsync("smtp.yandex.ru", 25, false);
                            await client.AuthenticateAsync("YourEmail", "YourPassword");
                            await client.SendAsync(emailMessage);
                            await client.DisconnectAsync(true);
                        }
                        TempData["Message"] = "Succesfull";
                    }
                    else
                    {
                        ViewData["Message"] = "Criminal";
                        return View();
                    }
                }
                else
                {
                    ViewData["Message"] = "Fool";
                    return View();
                }

            }
            finally { await con.CloseAsync(); }
            return RedirectToAction("Index");
        }
        public IActionResult Entry()
        {
            return View();
        }
        public IActionResult AboutDeveloper()
        {
            return View();
        }

        public async Task<IActionResult> Macroses(string id1)
        {
            try
            {
                ViewData["Message"] = TempData["Message"];
                listComment.Clear();
                await con.OpenAsync();
                if (HttpContext.User.Identity.IsAuthenticated && id1 != null)
                {
                    
                    command = new MySqlCommand($"INSERT INTO Comment(UserName,Photo,Article,ViewPage,Evaluate) values ('{User.FindFirst(ClaimTypes.Name)?.Value + "|" + User.FindFirst(ClaimTypes.Email)?.Value.Split('@')[0]}','{User.FindFirst("Photo")?.Value}','{id1}',\'Macroses\',{Program.UseModelWithSingleItem(Program.mlContext, Program.model, id1)})", con);
                    command.ExecuteNonQuery();
                }
                await ReturnComment("Macroses");
            }
            finally { await con.CloseAsync(); }
            return View(listComment);
        }
        public async Task<IActionResult> Assembler(string id1)
        {
            try
            {
                ViewData["Message"] = TempData["Message"];
                listComment.Clear();
                await con.OpenAsync();
                if (HttpContext.User.Identity.IsAuthenticated && id1 != null)
                {
                    command = new MySqlCommand($"INSERT INTO Comment(UserName,Photo,Article,ViewPage,Evaluate) values ('{User.FindFirst(ClaimTypes.Name)?.Value + "|" + User.FindFirst(ClaimTypes.Email)?.Value.Split('@')[0]}','{User.FindFirst("Photo")?.Value}','{id1}',\'Assembler\',{Program.UseModelWithSingleItem(Program.mlContext, Program.model, id1)})", con);
                    command.ExecuteNonQuery();
                }
                await ReturnComment("Assembler");
            }
            finally { await con.CloseAsync(); }
            return View(listComment);
        }
        public async Task<IActionResult> KontrollerDany(string id1)
		{
			try
			{
				ViewData["Message"] = TempData["Message"];
				listComment.Clear();
				await con.OpenAsync();
				if (HttpContext.User.Identity.IsAuthenticated && id1 != null)
				{
                    command = new MySqlCommand($"INSERT INTO Comment(UserName,Photo,Article,ViewPage,Evaluate) values ('{User.FindFirst(ClaimTypes.Name)?.Value + "|" + User.FindFirst(ClaimTypes.Email)?.Value.Split('@')[0]}','{User.FindFirst("Photo")?.Value}','{id1}',\'KontrollerDany\',{Program.UseModelWithSingleItem(Program.mlContext, Program.model, id1)})", con);
                    command.ExecuteNonQuery();
				}
				await ReturnComment("KontrollerDany");
			}
			finally { await con.CloseAsync(); }
			return View(listComment);
		}

		public async Task<IActionResult> Article()
        {
            try
            {
                
                listComment.Clear();
                await con.OpenAsync();
                command = new MySqlCommand("SELECT UserName,Photo,Article FROM Comment ", con);
                reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                        listComment.Add(new Person { Name = reader.GetValue(0).ToString().Split('|')[0], Photo = reader.GetValue(1).ToString(), Article = reader.GetValue(2).ToString() }); 
                }

            }
            finally { await con.CloseAsync(); }
            return View(listComment);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Entry([Bind("Email", "Password")] Person pers)
        {
            try
            {
                if (pers.Email != null && pers.Password != null)
                {
                    if (pers.Password.Length < 50 && pers.Email.Length < 100)
                    {
                        await con.OpenAsync();
                        command = new MySqlCommand($"SELECT Photo,Name,Lastname,Password,Email,AboutMyself FROM Users Where Email=\'{pers.Email}\' AND Password=\'{pers.Password}\'", con);
                        reader = command.ExecuteReader();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                string result = string.Join(" ", new string[2] { reader?.GetValue(1).ToString(), reader.GetValue(2).ToString() });
                                await Authenticate(reader.GetValue(0).ToString(), result, pers.Email, reader.GetValue(3).ToString(), reader.GetValue(5).ToString());
                            }
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            ViewData["Message"] = "Fool";
                            return View();
                        }
                    }
                    else
                    {
                        ViewData["Message"] = "Fool";
                        return View();
                    }
                }
                else
                {
                    ViewData["Message"] = "Fool";
                    return View();
                }
            }
            finally { await con.CloseAsync(); await reader.CloseAsync(); }

        }
        public IActionResult ProfileUser()
        {
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Index");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProfileUser(Person user, string? id1)
        {
            try
            {
                await con.OpenAsync();
                var identty = new ClaimsIdentity(User.Identity);
                if (user.Password != null)
                    if (user.MyFile != null)
                    {
                        string path = Directory.GetCurrentDirectory() + @"\wwwroot\img\" + User.FindFirst("Email")?.Value.Split('@')[0] + user.MyFile.FileName;
                        using (var folder = new FileStream(Path.Combine(path), FileMode.Create, FileAccess.Write)) //FileMode.Create - указывает, что ОС должна создать новый файл, а если он существует, то перезаписать благодаря FileAccess.Write.
                        {
                            await user.MyFile.CopyToAsync(folder);
                        }
                        FileInfo fileInf = new FileInfo(Directory.GetCurrentDirectory() + @"\wwwroot" + User.FindFirst("Photo")?.Value.Replace("/", @"\"));
                        if (fileInf.Exists && User.FindFirst("Photo")?.Value != "/img/DefaultPhoto.jpg")
                        {
                            fileInf.Delete();
                        }
                        command = new MySqlCommand($"UPDATE Users SET Photo=\'{"/img/" + User.FindFirst("Email")?.Value.Split('@')[0] + user.MyFile.FileName}\' Where Email=\'{User.FindFirst("Email")?.Value}\'", con);
                        command.ExecuteNonQuery();

                        identty.RemoveClaim(identty.FindFirst("Photo"));
                        identty.AddClaim(new Claim("Photo", "/img/" + User.FindFirst("Email")?.Value.Split('@')[0] + user.MyFile.FileName));
                        Console.WriteLine("/img/" + User.FindFirst("Email")?.Value.Split('@')[0] + user.MyFile.FileName);
                        var claimPrincipal = new ClaimsPrincipal(identty);
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimPrincipal);

                    }
                    else
                    {
                        if (id1 != null)
                        {
                            command = new MySqlCommand($"UPDATE Users SET AboutMyself=\'{id1}\' Where Email=\'{User.FindFirst("Email")?.Value}\'", con);
                            command.ExecuteNonQuery();
                            identty.RemoveClaim(identty.FindFirst("AboutMyself")); //тут всё-то менять не не надо, только то, что необходимо
                            identty.AddClaim(new Claim("AboutMyself", id1));
                        }
                        command = new MySqlCommand($"UPDATE Users SET Password=\'{user.Password}\' Where Email=\'{User.FindFirst("Email")?.Value}\'", con);
                        command.ExecuteNonQuery();
                        identty.RemoveClaim(identty.FindFirst("Password"));
                        identty.AddClaim(new Claim("Password", user.Password));
                        var claimPrincipal = new ClaimsPrincipal(identty);
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimPrincipal);
                    }

                return RedirectToAction("ProfileUser","Home"); //нужно обязательно перезагружатиь страницу, чтобы всё работало моментом

            }
            finally
            {
                await con.CloseAsync();
            }
        }
        public IActionResult Game()
        {
            if (User.Identity.IsAuthenticated == true)

            { Person color = new Person(); color.indicator = false; return View(color); }
            else
                return RedirectToAction("Index");
        }
        public static List<string> StaticColor = new List<string>();
        [HttpPost]
        public async Task<IActionResult> Game(Person color12)
        {
            try
            {
                var identty = new ClaimsIdentity(User.Identity);
                if (Convert.ToBoolean(User.FindFirst("ShowResult")?.Value) == true)
                {
                    identty.RemoveClaim(identty.FindFirst("ShowResult"));
                    identty.AddClaim(new Claim("ShowResult", "false"));
                    var claimPrincipal = new ClaimsPrincipal(identty);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimPrincipal);
                }
                await con.OpenAsync();
                if (color12.FirstArray != null)
                {
                    if (User.HasClaim(x => x.Type == "Color0") == false) //тут StaticColor==0
                    {
                        for (int i = 0; i < Convert.ToInt16(identty.FindFirst("Quantity").Value); i++)
                            identty.AddClaim(new Claim("Color" + i.ToString(), color12.FirstArray[i]));

                        StaticColor = color12.FirstArray;
                        identty.RemoveClaim(identty.FindFirst("Indicator"));
                        identty.AddClaim(new Claim("Indicator", "true"));
                        var claimPrincipal = new ClaimsPrincipal(identty);
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimPrincipal);
                    }
                    else
                    {

                        int score = 0;
                        for (int i = 0; i < Convert.ToInt16(User.FindFirst("Quantity")?.Value); i++)
                            if (identty.FindFirst("Color" + i.ToString()).Value == color12.FirstArray[i])
                                ++score;

                        for (int i = 0; i < Convert.ToInt16(identty.FindFirst("Quantity").Value); i++)
                            if (User.HasClaim(x => x.Type == "Color" + i.ToString()))
                            {
                                identty.RemoveClaim(identty.FindFirst("Color" + i.ToString()));
                            }

                        if (Convert.ToInt16(User.FindFirst("Level")?.Value) == 5)
                        {
                            //тут идёт обращение к базе, куда будут записываться дата и кол-во баллов участника
                            //в клаим объект нужно добавить level and score

                            if (identty.HasClaim(x => x.Type == "ReservScore")) //точно работает
                            {
                                identty.RemoveClaim(identty.FindFirst("ReservScore"));
                            }
                            identty.AddClaim(new Claim("ReservScore", identty.FindFirst("Score")?.Value.ToString()));
                            identty.RemoveClaim(identty.FindFirst("Score"));
                            identty.RemoveClaim(identty.FindFirst("Quantity"));
                            identty.RemoveClaim(identty.FindFirst("Level"));
                            identty.RemoveClaim(identty.FindFirst("Indicator"));
                            identty.RemoveClaim(identty.FindFirst("ShowResult"));
                            string result = "Баллов:" + Convert.ToString(User.FindFirst("Score")?.Value) + "  Дата:" + Convert.ToString(DateTime.Now);
                            command = new MySqlCommand($"INSERT INTO history(Email,Photo,UserName,result) values (\'{User.FindFirst("Email")?.Value}\',\'{User.FindFirst("Photo")?.Value}\',\'{User.FindFirst(ClaimTypes.Name)?.Value}\',\'{result}\')", con);
                            command.ExecuteNonQuery();
                            identty.AddClaim(new Claim("Score", "0"));
                            identty.AddClaim(new Claim("Quantity", "4"));
                            identty.AddClaim(new Claim("Level", "1"));
                            identty.AddClaim(new Claim("Indicator", "false"));
                            identty.AddClaim(new Claim("ShowResult", "true"));
                            var claimPrincipal = new ClaimsPrincipal(identty);
                            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimPrincipal);


                        }
                        else
                        {
                            int level = Convert.ToInt16(User.FindFirst("Level")?.Value);
                            level++;
                            int quantity = Convert.ToInt16(User.FindFirst("Quantity")?.Value);
                            quantity += 2;
                            int scroreReserv = score + Convert.ToInt16(identty.FindFirst("Score").Value);
                            identty.RemoveClaim(identty.FindFirst("Score"));
                            identty.RemoveClaim(identty.FindFirst("Quantity"));
                            identty.RemoveClaim(identty.FindFirst("Level"));
                            identty.RemoveClaim(identty.FindFirst("Indicator"));
                            identty.AddClaim(new Claim("Score", scroreReserv.ToString()));
                            identty.AddClaim(new Claim("Quantity", quantity.ToString()));
                            identty.AddClaim(new Claim("Level", level.ToString()));
                            identty.AddClaim(new Claim("Indicator", "false"));
                            var claimPrincipal = new ClaimsPrincipal(identty);
                            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimPrincipal);

                        }
                    }
                }
                else
                {
                    if (User.HasClaim(x => x.Type == "Color0")) //тут StaticColor==0
                    {
                        for (int i = 0; i < Convert.ToInt16(identty.FindFirst("Quantity").Value); i++)
                            if (User.HasClaim(x => x.Type == "Color" + i.ToString()))
                            {
                                identty.RemoveClaim(identty.FindFirst("Color" + i.ToString()));
                            }
                        var claimPrincipal = new ClaimsPrincipal(identty);
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimPrincipal);
                    }

                }
            }
            finally
            {
                await con.CloseAsync();
                await reader.CloseAsync();
            }

            //return View("Game",color12);
            return RedirectToAction("Game","Home");
            
        }

        public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); return RedirectToAction("Index"); }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public async Task Authenticate(string Photo, string Name, string UserMail, string Password, string AboutMyself)
        {
            var ClaimMail = new Claim("Email", UserMail);
            var ClaimName = new Claim(ClaimTypes.Name, Name);
            var ClaimPhoto = new Claim("Photo", Photo);
            var ClaimPassword = new Claim("Password", Password);
            var ClaimAboutMyself = new Claim("AboutMyself", AboutMyself);
            var ClaimScore = new Claim("Score", "0");
            var ClaimLevel = new Claim("Level", "1");
            var ClaimQuantity = new Claim("Quantity", "4");
            var ClaimNoWhite= new Claim("Indicator", "false");
            var ClaimShowResult = new Claim("ShowResult", "false");
            List<Claim> claims = new List<Claim> { ClaimMail, ClaimName, ClaimPhoto, ClaimPassword, ClaimAboutMyself, ClaimScore, ClaimLevel, ClaimQuantity, ClaimNoWhite, ClaimShowResult };
            var claimIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimPrincipal = new ClaimsPrincipal(claimIdentity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimPrincipal);
        }
    }
}