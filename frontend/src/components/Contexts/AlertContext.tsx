import { createContext, FC, ReactNode, useContext, useEffect, useState } from 'react'
import { addMinutes, max } from 'date-fns'
import { MissionStatus } from 'models/Mission'
import { FailedMissionAlertContent } from 'components/Alerts/FailedMissionAlert'
import { BackendAPICaller } from 'api/ApiCaller'
import { refreshInterval } from 'components/Pages/FrontPage/FrontPage'

export enum AlertSource {
    MissionFail,
    RequestFail
}

type AlertDictionaryType = { [key in AlertSource]?: {content: ReactNode | undefined, dismissFunction: () => void} }

interface IAlertContext {
    alerts: AlertDictionaryType
    setAlert: (source: AlertSource, alert: ReactNode, dismissFunction: () => void) => void
    cleartAlerts: () => void
    clearAlert: (source: AlertSource) => void
}

interface Props {
    children: React.ReactNode
}

const defaultAlertInterface = {
    alerts: {},
    setAlert: (source: AlertSource, alert: ReactNode, dismissFunction: () => void) => {},
    cleartAlerts: () => {},
    clearAlert: (source: AlertSource) => {}
}

export const AlertContext = createContext<IAlertContext>(defaultAlertInterface)

export const AlertProvider: FC<Props> = ({ children }) => {
    const [alerts, setAlerts] = useState<AlertDictionaryType>(defaultAlertInterface.alerts)

    const setAlert = (source: AlertSource, alert: ReactNode, dismissFunction: () => void) => {
        let newAlerts = { ...alerts }
        newAlerts[source] = {content: alert, dismissFunction: dismissFunction}
        setAlerts(newAlerts)
    }

    const cleartAlerts = () => {
        setAlerts({})
    }

    const clearAlert = (source: AlertSource) => {
        let newAlerts = { ...alerts }
        delete newAlerts[source]
        setAlerts(newAlerts)
    }

    // Here we update the recent failed missions
    const dismissMissionFailTimeKey: string = 'lastMissionFailDismissalTime'
    
    const dismissCurrentMissions = () => {
        sessionStorage.setItem(dismissMissionFailTimeKey, JSON.stringify(Date.now()))
        clearAlert(AlertSource.MissionFail)
    }

    useEffect(() => {
        const pageSize: number = 100
        // The default amount of minutes in the past for failed missions to generate an alert
        const defaultTimeInterval: number = 10
        // The maximum amount of minutes in the past for failed missions to generate an alert
        const maxTimeInterval: number = 60

        const getLastDismissalTime = (): Date => {
            const sessionValue = sessionStorage.getItem(dismissMissionFailTimeKey)
            if (sessionValue === null || sessionValue === '') {
                return addMinutes(Date.now(), -defaultTimeInterval)
            } else {
                // If last dismissal time was more than {MaxTimeInterval} minutes ago, use the limit value instead
                return max([addMinutes(Date.now(), -maxTimeInterval), JSON.parse(sessionValue)])
            }
        }

        const id = setInterval(() => {
            const lastDismissTime: Date = getLastDismissalTime()
            BackendAPICaller.getMissionRuns({ statuses: [MissionStatus.Failed], pageSize: pageSize }).then((missions) => {
                const newRecentFailedMissions = missions.content.filter((m) => new Date(m.endTime!) > lastDismissTime)
                if (newRecentFailedMissions.length > 0)
                    setAlert(AlertSource.MissionFail, <FailedMissionAlertContent missions={newRecentFailedMissions} />, dismissCurrentMissions)
            })
        }, refreshInterval)
        return () => clearInterval(id)
    }, [])

    return (
        <AlertContext.Provider
            value={{
                alerts,
                setAlert,
                cleartAlerts,
                clearAlert
            }}
        >
            {children}
        </AlertContext.Provider>
    )
}

export const useAlertContext = () => useContext(AlertContext)
