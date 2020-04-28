using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.SessionState;
using System.Web.WebSockets;

namespace websocketApplication
{
    /// <summary>
    /// webSocketHandler 的摘要说明
    /// </summary>
    public class webSocketHandler : IHttpHandler, IRequiresSessionState
    {
          //用户登记标识
        private string userKey = "";
        public void ProcessRequest(HttpContext context)
        {
            if (context.IsWebSocketRequest)
            {
                this.userKey = context.Request.QueryString["userKey"]; //获取url后面的参数值>>登陆的userKey
                context.AcceptWebSocketRequest(ProcessChat);
            }
            else
            {
                context.Response.Write("不是WebSocket请求");
            }
        }
        //ProcessChat进了此方法说明连接成功
         private async Task ProcessChat(AspNetWebSocketContext context)
        {
            WebSocket socket = context.WebSocket;   //  WebSocket实例对象包含状态state==open 
            CancellationToken cancellationToken = new CancellationToken(); //Token?
            //登陆 》》获取用户的状态 
            bool isExits = WebManager.AddUser(userKey, socket); //WebManager去添加用户
            if (isExits == false)
            {
                //表示该用户有在线
                await WebManager.SendToMySelf(cancellationToken, "用户"+this.userKey+" 已在线", this.userKey);
            }
            else
            {
                //表示登录成功
                //某人登陆后，给群里其他人发送提示信息
                await WebManager.SendLoginSucesssNotice(cancellationToken, "用户"+this.userKey+" 进入聊天室,当前时间："+DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                while (socket.State == WebSocketState.Open)
                {
                    ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[2048]);
                    //接受指令
                    WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, cancellationToken);
                    //表示是关闭指令
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        //移除该用户对应的socket对象
                        WebManager.RemoveUser(userKey);
                        //发送离开提醒
                        await WebManager.SendOutNotice(cancellationToken, "用户"+this.userKey+" 离开聊天室,当前时间："+DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, String.Empty, cancellationToken);
                    }
                    //获取是发送消息指令
                    else
                    {
                        string userMsg = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);//前端传过来的值msg?
                        Content content = new Content();
                        //这里要区分是单发还是群发(通过与前端在内容开头做一个标记，来区分是单发还是群发)
                        if (userMsg.Length > 8 && userMsg.Substring(0, 8) == "$--$--**")
                        {
                            //表示是单发，截取内容和接受者的标记
                            var array = userMsg.Split(new string[] { "$--$--**" }, StringSplitOptions.None);
                            var receiveNotice = array[1];  //发送给谁
                         //   string content = "用户"+this.userKey+" 发来消息："+array[2]+",当前时间："+DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            content.msgText = array[2];
                            WebManager.SendSingleMessage(cancellationToken, userKey, receiveNotice);
                        }
                        else
                        {
                            //表示群发信息 
                          //  string content = "用户"+this.userKey+" 群发消息："+userMsg+",当前时间："+DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                          
                          content.msgText = userMsg;
                            WebManager.SendAllMessage(cancellationToken, userKey);
                        }
                    }
                }
            }

        }

         /// <summary>
        ///  true 如果 System.Web.IHttpHandler 实例是可重复使用; 否则为 false。
        /// </summary>
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}