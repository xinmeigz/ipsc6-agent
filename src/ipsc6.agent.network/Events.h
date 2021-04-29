#pragma once

#include "AgentMessage.h"

using namespace System;

namespace ipsc6 {
namespace agent {
namespace network {

public
delegate void ConnectAttemptFailedEventHandler(Object ^ sender);

public
delegate void DisconnectedEventHandler(Object ^ sender);

public
delegate void ConnectionLostEventHandler(Object ^ sender);

public
ref class ConnectedEventArgs : EventArgs {
   public:
    ConnectedEventArgs() : EventArgs(){};
};

public
delegate void ConnectedEventHandler(Object ^ sender, ConnectedEventArgs ^ e);

public
ref class AgentMessageReceivedEventArgs : EventArgs {
   public:
    property AgentMessage CommandType;
    property int N1;
    property int N2;
    property String ^ S;
    AgentMessageReceivedEventArgs() : EventArgs(){};
    AgentMessageReceivedEventArgs(AgentMessage commandType,
                                  int n1,
                                  int n2,
                                  String ^ s)
        : EventArgs() {
        CommandType = commandType;
        N1 = n1;
        N2 = n2;
        S = s;
    };
    virtual String ^ ToString() override;
};

public
delegate void AgentMessageReceivedEventHandler(Object ^ sender,
                                               AgentMessageReceivedEventArgs ^
                                                   e);

}  // namespace network
}  // namespace agent
}  // namespace ipsc6