﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Protocol_01
{
    public class MessageUtil
    {
        async public static void Send(Stream stream, Message message)
        {
            stream.WriteAsync(message.GetBytes());
        }

        // 수신 -> COMMAND분석 -> COM에 따른 처리 메소드호출 -> 응답할 메세지 반환
        // 각 COMMAND에 따른 ACK, NCK를 반환할 메소드를 매개변수로 받는다.
        async public static 
        // 수신 -> COMMAND분석 -> COM에 따른 처리 메소드호출 -> 응답할 메세지 반환
        // 각 COMMAND에 따른 ACK, NCK를 반환할 메소드를 매개변수로 받는다.
        Task
Receive(object o, Func<bool> startResult, Func<bool> endResult, Func<string, string, Value> requestResult, Func<string, string, bool> rotationResult, Action<string, string> writeLog)
        {
            TcpClient tc = (TcpClient)o;

            int MAX_SIZE = 1024;
            NetworkStream stream = tc.GetStream();
            string msg = "";
            Message respMsg = null;
            var buff = new byte[MAX_SIZE];  
            var nbytes = await stream.ReadAsync(buff, 0, buff.Length).ConfigureAwait(false);
            if(nbytes > 0)
            {
                msg = Encoding.UTF8.GetString(buff, 0, nbytes).Substring(1, msg.Length);

                // 받은메세지 로그에 출력
                writeLog(msg, "rec");

                if (msg.Substring(0) != "<" || msg.Substring(msg.Length, msg.Length + 1) != ">")
                {
                    // STX, ETX 가 잘못된경우 null을 반환
                    writeLog(respMsg.ToString(), "resp");
                }
                else
                {
                    string[] splitMsg = msg.Substring(1, msg.Length).Split(","); // STX, ETX 제거 맟 ','로 프로토콜 요소 분리

                    // COMMAND 별로 각 메소드 호출해서 응답할 값을 받아온다.
                    // respMsg = StartResp();
                    switch (splitMsg[2].Substring(0, 3))
                    {
                        case "STA":
                            respMsg = GetStartResp(splitMsg, startResult);
                            break;
                        case "END":
                            respMsg = GetEndResp(splitMsg, endResult);
                            break;
                        case "REQ":
                            respMsg = GetRequestResp(splitMsg, requestResult);
                            break;
                        case "ROT":
                            respMsg = GetRotationResp(splitMsg, rotationResult);
                            break;
                    }

                    // 보낼메세지 로그에 출력
                    writeLog(respMsg.ToString(), "resp");
                }

                // 송신자에게 결과 메세지 응답
                Send(stream, respMsg);
            }
            stream.Close();
            tc.Close();
        }

        public static Message GetStartResp(string[] message, Func<bool> startResult)
        {
            Message msg;
            Base startBase = new Base(CONSTANTS.PICKER_ITEM.P0.ToString(), CONSTANTS.VISION_ITEM.ALL.ToString(), message[2]);
            Data startData;
            
            if(startResult.Invoke() == true)
            {
                // 1. ACK
                startData = new Data(CONSTANTS.SUCCESS);
                msg = new Message(startBase, startData);
            }
            else
            {
                // 2. NCK
                startData = new Data(CONSTANTS.FAIL);
                Error startError = new Error(CONSTANTS.ERROR);
                msg = new Message(startBase, startData, startError);
            }

            return msg;
        }

        public static Message GetEndResp(string[] message, Func<bool> endResult)
        {
            Message msg;
            Base endBase = new Base(CONSTANTS.PICKER_ITEM.P0.ToString(), CONSTANTS.VISION_ITEM.ALL.ToString(), message[2]);
            Data endData;

            if(endResult.Invoke() == true)
            {
                // 1. ACK
                endData = new Data(CONSTANTS.SUCCESS);
                msg = new Message(endBase, endData);
            }
            else
            {
                // 2. NCK
                endData = new Data(CONSTANTS.FAIL);
                Error endError = new Error(CONSTANTS.ERROR);
                msg = new Message(endBase, endData, endError);
            }

            return msg;
        }

        public static Message GetRequestResp(string[] message, Func<string, string, Value> requestResult)
        {
            Message msg;
            Base requestBase = new Base(message[0], message[1], message[2]);
            Data requestData;

            string tempNo = message[0].Substring(1);

            string pickerNo = Enum.GetName(typeof(CONSTANTS.PICKER_ITEM), "P" + tempNo);
            string visionName = Enum.GetName(typeof(CONSTANTS.VISION_ITEM), message[1]);

            Value requestValue;

            if ((requestValue = requestResult.Invoke(pickerNo, visionName)).X != "") // 값이 null이 아니면 ACK
            {
                requestData = new Data(CONSTANTS.SUCCESS);
                msg = new Message(requestBase, requestData, requestValue);
            }
            else
            {
                requestData = new Data(CONSTANTS.FAIL);
                Error requestError = new Error(CONSTANTS.ERROR);
                msg = new Message(requestBase, requestData, requestError);
            }

            return msg;
        }

        public static Message GetRotationResp(string[] message, Func<string, string, bool> rotationResult)
        {

            Message msg;
            Base rotationBase = new Base(message[0], message[1], message[2]);
            Data rotationData;

            string pickerNo = message[0].Substring(1);
            string rotationNo = message[3].Substring(8);

            if (rotationResult.Invoke(pickerNo, rotationNo) == true)
            {
                rotationData = new Data(CONSTANTS.SUCCESS);
                msg = new Message(rotationBase, rotationData);
            }
            else
            {
                rotationData = new Data(CONSTANTS.FAIL);
                Error rotationError = new Error(CONSTANTS.ERROR);
                msg = new Message(rotationBase, rotationData, rotationError);
            }

            return msg;
        }
    }
}