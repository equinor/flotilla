import * as signalR from "@microsoft/signalr";
import { useIsAuthenticated } from '@azure/msal-react'
import { createContext, FC, useContext, useEffect, useState } from 'react'
import { AuthContext } from "./AuthProvider";
import { config } from 'config'

interface ISignalRContext {
    connection: signalR.HubConnection | undefined
    registerEvent: (eventName: string, onMessageReceived: (username: string, message: string) => void) => void
    connectionReady: boolean
}

interface Props {
    children: React.ReactNode
}

const defaultSignalRInterface = {
    connection: undefined,
    registerEvent: (eventName: string, onMessageReceived: (username: string, message: string) => void) => {},
    connectionReady: false
}

const URL = config.BACKEND_API_SIGNALR_URL

export const SignalRContext = createContext<ISignalRContext>(defaultSignalRInterface)

export const SignalRProvider: FC<Props> = ({ children }) => {
    const [connection, setConnection] = useState<signalR.HubConnection | undefined>(defaultSignalRInterface.connection)
    const [connectionReady, setConnectionReady] = useState<boolean>(defaultSignalRInterface.connectionReady)
    const isAuthenticated = useIsAuthenticated()
    const accessToken = useContext(AuthContext)

    useEffect(() => {
        if (isAuthenticated && accessToken) {
            var newConnection = new signalR.HubConnectionBuilder()
                .withUrl(URL, { accessTokenFactory: () => accessToken})
                .withAutomaticReconnect()
                .build()

                newConnection.start().then(() => {
                    console.log("SignalR connection made: ", newConnection)
                    setConnection(newConnection)
                    setConnectionReady(true)
                }).catch(err => document.write(err))
        }
    }, [isAuthenticated, accessToken])

    const registerEvent = (eventName: string, onMessageReceived: (username: string, message: string) => void) => {
        if (connection) {
            connection.on(eventName, (username, message) => {
                onMessageReceived(username, message);
            });
        }
    }

    return (
        <SignalRContext.Provider
            value={{
                connection,
                registerEvent,
                connectionReady
            }}
        >
            {children}
        </SignalRContext.Provider>
    )
}

export const useSignalRContext = () => useContext(SignalRContext)
