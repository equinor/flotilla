import * as signalR from '@microsoft/signalr'
import React, { createContext, FC, useContext, useEffect, useCallback, useState } from 'react'
import { AuthContext } from './AuthContext'
import { config } from 'config'
import { useMsal } from '@azure/msal-react'

/**
 * SignalR provides asynchronous communication between backend and frontend. This
 * context provides functions for establishing event listeners and for accessing
 * the connection object, primarily to verify that a connection has been made.
 *
 * When registering an event handler using "registerEvent" an event name needs to be
 * provided, which must correspond to the event name used on the backend. The event
 * handler should receive a username and a message, though the username is typically
 * not relevant for broadcasted messages.
 *
 * It is important to note that event handlers can only see the scope at which they
 * are defined, which means that any React state will be outdated once they receive
 * a message. It is therefore important to update React state within these handlers
 * by passing functions in the setter functions. For instance instead of doing:
 *
 *   setState({...state, newEntry})
 *
 * we must instead do:
 *
 *   setState((oldState) => { return {...oldState, newEntry} })
 *
 * When accessing this context within another context, it is also important to make
 * sure that the other context provider is nested within the signalR context
 * provider.
 *
 * Objects are received as JSON strings. When parsing these it is important to note
 * that enums are typically encoded as numbers on the backend, and may therefore
 * need to be translated to string enums when making comparisons on the frontend.
 */

interface ISignalRContext {
    registerEvent: (eventName: string, onMessageReceived: (username: string, message: string) => void) => void
    connectionReady: boolean
    resetConnection: () => void
}

interface Props {
    children: React.ReactNode
}

const defaultSignalRInterface = {
    registerEvent: () => {},
    connectionReady: false,
    resetConnection: () => {},
}

const URL = config.BACKEND_API_SIGNALR_URL

const SignalRContext = createContext<ISignalRContext>(defaultSignalRInterface)

export const SignalRProvider: FC<Props> = ({ children }) => {
    const [connection, setConnection] = useState<signalR.HubConnection | undefined>(undefined)
    const [connectionReady, setConnectionReady] = useState<boolean>(defaultSignalRInterface.connectionReady)
    const { getAccessToken } = useContext(AuthContext)
    const { accounts, inProgress } = useMsal()

    const createConnection = useCallback(() => {
        console.log('Attempting to create signalR connection...')
        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl(URL, {
                accessTokenFactory: async () => {
                    try {
                        return await getAccessToken()
                    } catch (e) {
                        console.error('Failed to acquire access token for SignalR:', e)
                        return '' // causes auth to fail; connection will error/retry
                    }
                },
                transport:
                    signalR.HttpTransportType.WebSockets |
                    signalR.HttpTransportType.ServerSentEvents |
                    signalR.HttpTransportType.LongPolling,
            })
            .withAutomaticReconnect()
            .build()

        newConnection.onclose((error) => {
            console.log('SignalR connection closed:', error)
            setConnectionReady(false)
        })

        newConnection.onreconnected(() => {
            console.log('SignalR reconnected')
            setConnectionReady(true)
        })

        newConnection.onreconnecting(() => {
            console.log('SignalR reconnecting...')
            setConnectionReady(false)
        })

        return newConnection
    }, [getAccessToken])

    const resetConnection = () => {
        if (connection) {
            connection.stop()
        }

        const newConnection = createConnection()
        setConnection(newConnection)

        newConnection
            .start()
            .then(() => {
                console.log('SignalR connection made: ', newConnection)
                setConnectionReady(true)
            })
            .catch((error) => {
                console.error('SignalR connection failed:', error)
                setConnectionReady(false)
            })
        return newConnection
    }

    useEffect(() => {
        if (!accounts[0] || inProgress !== 'none') return

        const newConnection = resetConnection()

        return () => {
            newConnection.stop()
        }
    }, [accounts, inProgress])

    const registerEvent = (eventName: string, onMessageReceived: (username: string, message: string) => void) => {
        if (connection)
            connection.on(eventName, (username, message) => {
                if (message === 'null') {
                    console.warn(`Received signalR message for event ${eventName} is 'null'`)
                    return
                }
                onMessageReceived(username, message)
            })
    }

    return (
        <SignalRContext.Provider
            value={{
                registerEvent,
                connectionReady,
                resetConnection,
            }}
        >
            {children}
        </SignalRContext.Provider>
    )
}

export const useSignalRContext = () => useContext(SignalRContext)

export enum SignalREventLabels {
    missionRunUpdated = 'Mission run updated',
    missionDefinitionCreated = 'Mission definition created',
    missionDefinitionUpdated = 'Mission definition updated',
    missionDefinitionDeleted = 'Mission definition deleted',
    missionRunCreated = 'Mission run created',
    missionRunDeleted = 'Mission run deleted',
    missionRunFailed = 'Mission run failed',
    inspectionAreaCreated = 'InspectionArea created',
    inspectionAreaUpdated = 'InspectionArea updated',
    inspectionAreaDeleted = 'InspectionArea deleted',
    robotAdded = 'Robot added',
    robotUpdated = 'Robot updated',
    robotPropertyUpdated = 'Robot property updated',
    robotTelemetryUpdated = 'Robot telemetry updated',
    robotDeleted = 'Robot deleted',
    inspectionUpdated = 'Inspection updated',
    alert = 'Alert',
    mediaStreamConfigReceived = 'Media stream config received',
    inspectionVisualizationReady = 'Inspection Visulization Ready',
    analysisResultReady = 'Analysis Result Ready',
}
