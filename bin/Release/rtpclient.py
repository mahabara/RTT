# -*- coding: utf-8 -*-
#Created on 2015��4��9��

#@author: etbcffg

#
# -*- coding: cp936 -*-





from socket import *


class client:
    ip = '127.0.0.1' 
    port = 18001
     
    def __init__(self):
        print 'RTP client obj imported'

    def address(self,IP,port):
        self.ip = IP
        self.port = port
        print 'Server address:',self.ip,':',self.port
       
    def commandline(self):
        client = socket(AF_INET,SOCK_STREAM)
        addr=(self.ip, self.port)
        while True:
            command=raw_input("Please input comannds\n")
            if(command=="end"):
                break
            try:
                    #send=cmdlist[idx]+"$"+str(delay)+'\0'
                client.sendto(command,addr)
            except:
                (ErrorType, ErrorValue, ErrorTB) = sys.exc_info()
                print "Connect server failed: ", ErrorValue
                continue
            data="Reply from server:\n"
            try:
                data+=client.recv(25000)
            except:
                (ErrorType, ErrorValue, ErrorTB) = sys.exc_info()
                print "Connect server failed: ", ErrorValue
                continue
            print data
        client.close()

    def cmd(self,command,delay=0):
        client = socket(AF_INET,SOCK_STREAM)
        client.connect((self.ip, self.port))
        addr=(self.ip, self.port)
        cmdlist = command.split('\n')
          
        for idx in range(0,len(cmdlist)):
            try:
                sendcmd=cmdlist[idx]+"$"+str(delay)+'\0'
                client.send(sendcmd)
            except:
                (ErrorType, ErrorValue, ErrorTB) = sys.exc_info()
                return 'fail to send command to server'

            try:
                data=client.recv(25000)
            except:
                (ErrorType, ErrorValue, ErrorTB) = sys.exc_info()
                return 'fail to receive message from server'
                                
        else:
            pass
                    
        client.close()
        return data

    def pcmd(self,command,delay=0):
        client = socket(AF_INET,SOCK_DGRAM)
        addr=(self.ip, self.port)
        cmdlist = command.split('\n')
          
        for idx in range(0,len(cmdlist)):
            try:
                send=cmdlist[idx]+"$"+str(delay)+'\0'
                client.sendto(send,addr)
            except:
                (ErrorType, ErrorValue, ErrorTB) = sys.exc_info()
                return 'fail to send command to server'

            try:
                data=client.recv(25000)
            except:
                (ErrorType, ErrorValue, ErrorTB) = sys.exc_info()
                return 'fail to receive message from server'
                                
        else:
            pass
                    
        client.close()
        print data
        return data      

    
#---------------------------------------------------- def retvalstrip(retvalue):
    #---------------------------------------------------------- newretvalue = []
    #------------------------------------------------------ for ret in retvalue:
        #---------------------------------------------------------- if ret !='':
            #------------------------------------------- newretvalue.appent(ret)
    #-------------------------------------------------------- return newretvalue
#------------------------------------------------------------------------------ 
#----------------------------------------------------------------- class client:
#------------------------------------------------------------------------------ 
    #------------------------------------------------ def address(self,IP,port):
        #------------------------------------------------------------------ pass
#------------------------------------------------------------------------------ 
    #---------------------------------------------------- def commandline(self):
        #------------------------------------------------------------------ pass
#------------------------------------------------------------------------------ 
    #-------------------------------------------- def cmd(self,command,delay=0):
#------------------------------------------------------------------------------ 
        #--------------------------------------------------------- print command
        #--------- #�������ͷ�ж���������һ�ȡ��Ӧ��agent:��'SA.Command::SYST:PRESET'
        #------------------------------------------------------------- agent = 0
        # if 'SA.Command' in command:   #process SA commands:'SA.Command::SYST:PRESET'
            #------------------------ cmdstr = command.replace('SA.Command:','')
            # agent = deviceAgentFactory.get_DeviceAgent(constants.DEVICE_NAME_SIGNALANALYZER)
#------------------------------------------------------------------------------ 
        # if 'SG.Command' in command:   #process SG commands:'SG.Command:SOURce1:BB:EUTR:STAT ON'
            #------------------------ cmdstr = command.replace('SG.Command:','')
            # agent = deviceAgentFactory.get_DeviceAgent(constants.DEVICE_NAME_SIGNALGENERATOR)
#------------------------------------------------------------------------------ 
        # if 'ts.' in command or 'RRU.' in command:   #process ts commands:"ts.DL_Release"
            #-------------------------------------------------- cmdstr = command
            # agent = deviceAgentFactory.get_DeviceAgent(constants.DEVICE_NAME_RRU)
#------------------------------------------------------------------------------ 
        #-------- if 'pwr' in command:   #process ts commands:"pwr UL_OAB 0 0 4"
            #-------------------------------- cmdstr = command.replace('ts.','')
            # agent = deviceAgentFactory.get_DeviceAgent(constants.DEVICE_NAME_RRU)
#------------------------------------------------------------------------------ 
#------------------------------------------------------------------------------ 
        #------------------ if 'RF-Box.' in command:   ##process RF-BOX commands
            #---------------------------- cmdstr = command.replace('RF-Box.','')
            # agent = deviceAgentFactory.get_DeviceAgent(constants.DEVICE_NAME_RFBOX)
#------------------------------------------------------------------------------ 
        #------ if 'Process.Delay' in command:   #process 'Process.Delay(72000)'
            #-------------------------- delay = command.strip('Process.Delay()')
            #-------------------------- retvalue = 'delay ' + str(delay) + ' ms'
#------------------------------------------------------------------------------ 
            #------------------------------ time.sleep(float(delay)/float(1000))
        #------------------------------------------------------- return retvalue
#------------------------------------------------------------------------------ 
        #--------------------------------------------------------- #print cmdstr
        #------------------------------------ retvalue = agent.cmd(cmdstr,delay)
#------------------------------------------------------------------------------ 
#------------------------------------------------------------------------------ 
#------------------------------------------------------------------------------ 
        #------------------------------------------------------- return retvalue
#------------------------------------------------------------------------------ 
#------------------------------------------------------------------------------ 
#------------------------------------------------------------------------------ 
    #------------------------------------------- def pcmd(self,command,delay=0):
        #------------------------------------------------------------------ pass

     
