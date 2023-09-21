import { createContext, FC, ReactNode, useContext, useEffect, useState } from 'react'
import { addMinutes, max } from 'date-fns'
import { Mission, MissionStatus } from 'models/Mission'
import { FailedMissionAlertContent } from 'components/Alerts/FailedMissionAlert'
import { BackendAPICaller } from 'api/ApiCaller'
import { refreshInterval } from 'components/Pages/FrontPage/FrontPage'

export enum AlertType {
    MissionFail,
    RequestFail,
}

type AlertDictionaryType = { [key in AlertType]?: { content: ReactNode | undefined; dismissFunction: () => void } }

interface IAlertContext {
    alerts: AlertDictionaryType
    setAlert: (source: AlertType, alert: ReactNode) => void
    clearAlerts: () => void
    clearAlert: (source: AlertType) => void
}

interface Props {
    children: React.ReactNode
}

const defaultAlertInterface = {
    alerts: {},
    setAlert: (source: AlertType, alert: ReactNode) => {},
    clearAlerts: () => {},
    clearAlert: (source: AlertType) => {},
}

export const AlertContext = createContext<IAlertContext>(defaultAlertInterface)

export const AlertProvider: FC<Props> = ({ children }) => {
    const [alerts, setAlerts] = useState<AlertDictionaryType>(defaultAlertInterface.alerts)

    const dismissMissionFailTimeKey: string = 'lastMissionFailDismissalTime'

    const setAlert = (source: AlertType, alert: ReactNode) =>
        setAlerts({ ...alerts, [source]: { content: alert, dismissFunction: () => clearAlert(source) } })

    const clearAlerts = () => setAlerts({})

    const clearAlert = (source: AlertType) => {
        if (source === AlertType.MissionFail)
            sessionStorage.setItem(dismissMissionFailTimeKey, JSON.stringify(Date.now()))
        let newAlerts = { ...alerts }
        delete newAlerts[source]
        setAlerts(newAlerts)
    }

    // This variable is needed since the state in the useEffect below uses an outdated alert object
    const [newFailedMissions, setNewFailedMissions] = useState<Mission[]>([])

    // Here we update the recent failed missions
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
            BackendAPICaller.getMissionRuns({ statuses: [MissionStatus.Failed], pageSize: pageSize }).then(
                (missions) => {
                    const newRecentFailedMissions = missions.content.filter(
                        (m) => new Date(m.endTime!) > lastDismissTime
                    )
                    if (newRecentFailedMissions.length > 0) setNewFailedMissions(newRecentFailedMissions)
                }
            )
        }, refreshInterval)
        return () => clearInterval(id)
    }, [])

    useEffect(() => {
        if (newFailedMissions.length > 0) {
            setAlert(AlertType.MissionFail, <FailedMissionAlertContent missions={newFailedMissions} />)
            setNewFailedMissions([])
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [newFailedMissions])

    return (
        <AlertContext.Provider
            value={{
                alerts,
                setAlert,
                clearAlerts,
                clearAlert,
            }}
        >
            {children}
        </AlertContext.Provider>
    )
}

export const useAlertContext = () => useContext(AlertContext)
