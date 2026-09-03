import * as signalR from '@microsoft/signalr'
import React, { createContext, FC, useContext, useEffect, useCallback, useMemo, useState, useRef } from 'react'
import { AuthContext } from './AuthContext'
import { config } from 'config'
import { useMsal } from '@azure/msal-react'
import { useOnPageVisible } from 'hooks/usePageVisibility'
import { noopUnsubscribe, UnsubscribeFromEvent } from 'utils/signalR'

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
 * "registerEvent" returns an unsubscribe function. Callers MUST return it from their
 * effect cleanup, otherwise handlers accumulate on the shared connection every time
 * the effect re-runs (on reconnect, or when a component remounts on navigation) and
 * each stale handler keeps firing against an unmounted component.
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
    registerEvent: (
        eventName: string,
        onMessageReceived: (username: string, message: string) => void
    ) => UnsubscribeFromEvent
    connectionReady: boolean
}

interface Props {
    children: React.ReactNode
}

const defaultSignalRInterface = {
    registerEvent: () => noopUnsubscribe,
    connectionReady: false,
}

const URL = config.BACKEND_API_SIGNALR_URL

const SignalRContext = createContext<ISignalRContext>(defaultSignalRInterface)

export const SignalRProvider: FC<Props> = ({ children }) => {
    const connectionRef = useRef<signalR.HubConnection | undefined>(undefined)
    const [connectionReady, setConnectionReady] = useState<boolean>(defaultSignalRInterface.connectionReady)
    const { getBackendAccessToken } = useContext(AuthContext)
    const { accounts, inProgress } = useMsal()

    const createConnection = useCallback(() => {
        console.log('Attempting to create signalR connection...')
        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl(URL, {
                accessTokenFactory: async () => {
                    try {
                        return await getBackendAccessToken()
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
            .configureLogging(signalR.LogLevel.Critical)
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
    }, [getBackendAccessToken])

    const resetConnection = () => {
        if (connectionRef.current) {
            connectionRef.current.stop()
        }

        const newConnection = createConnection()
        connectionRef.current = newConnection

        newConnection
            .start()
            .then(() => {
                console.log('SignalR connection made: ', newConnection)
                setConnectionReady(true)
            })
            .catch((error) => {
                if (error instanceof Error && error.constructor?.name === 'AbortError') {
                    // Aborting a connection is expected when the user navigates away or the react dev remounts.
                    setConnectionReady(false)
                } else {
                    console.error('SignalR connection failed:', error)
                    setConnectionReady(false)
                }
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

    useOnPageVisible(() => {
        if (!accounts[0] || inProgress !== 'none') return
        if (connectionRef.current?.state === signalR.HubConnectionState.Connected) return
        resetConnection()
    })

    // Stable identity: consumers list registerEvent in their effect dependencies, so a
    // new identity on every render would re-subscribe them on every render.
    const registerEvent = useCallback(
        (eventName: string, onMessageReceived: (username: string, message: string) => void): UnsubscribeFromEvent => {
            const connection = connectionRef.current
            if (!connection) return noopUnsubscribe

            const handler = (username: string, message: string) => {
                if (message === 'null') {
                    console.warn(`Received signalR message for event ${eventName} is 'null'`)
                    return
                }
                onMessageReceived(username, message)
            }

            connection.on(eventName, handler)
            return () => connection.off(eventName, handler)
        },
        []
    )

    // Memoised so that a provider re-render does not hand consumers a new object and
    // re-trigger every effect that depends on this context.
    const contextValue = useMemo(() => ({ registerEvent, connectionReady }), [registerEvent, connectionReady])

    return <SignalRContext.Provider value={contextValue}>{children}</SignalRContext.Provider>
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
