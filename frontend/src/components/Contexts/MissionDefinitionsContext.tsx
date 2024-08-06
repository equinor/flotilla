import { createContext, FC, useContext, useEffect, useState } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { MissionDefinition } from 'models/MissionDefinition'
import { useInstallationContext } from './InstallationContext'
import { useLanguageContext } from './LanguageContext'
import { AlertType, useAlertContext } from './AlertContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'

interface IMissionDefinitionsContext {
    missionDefinitions: MissionDefinition[]
}

interface Props {
    children: React.ReactNode
}

const defaultMissionDefinitionsContext: IMissionDefinitionsContext = {
    missionDefinitions: [],
}

export const MissionDefinitionsContext = createContext<IMissionDefinitionsContext>(defaultMissionDefinitionsContext)

const upsertMissionDefinition = (oldQueue: MissionDefinition[], updatedMission: MissionDefinition) => {
    const oldQueueCopy = [...oldQueue]
    const existingIndex = oldQueueCopy.findIndex((m) => m.id === updatedMission.id)
    if (existingIndex !== -1) {
        oldQueueCopy[existingIndex] = updatedMission
        return oldQueueCopy
    } else {
        return [...oldQueueCopy, updatedMission]
    }
}

const fetchMissionDefinitions = (params: {
    installationCode: string
    pageSize: number
    orderBy: string
}): Promise<MissionDefinition[]> => BackendAPICaller.getMissionDefinitions(params).then((response) => response.content)

export const useMissionDefinitions = (): IMissionDefinitionsContext => {
    const [missionDefinitions, setMissionDefinitions] = useState<MissionDefinition[]>([])
    const { registerEvent, connectionReady } = useSignalRContext()
    const { installationCode } = useInstallationContext()
    const { TranslateText } = useLanguageContext()
    const { setAlert, setListAlert } = useAlertContext()

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.missionDefinitionUpdated, (username: string, message: string) => {
                const missionDefinition: MissionDefinition = JSON.parse(message)
                setMissionDefinitions((oldMissionDefinitions) =>
                    upsertMissionDefinition(oldMissionDefinitions, missionDefinition)
                )
            })
            registerEvent(SignalREventLabels.missionDefinitionCreated, (username: string, message: string) => {
                const missionDefinition: MissionDefinition = JSON.parse(message)
                setMissionDefinitions((oldMissionDefinitions) =>
                    upsertMissionDefinition(oldMissionDefinitions, missionDefinition)
                )
            })
            registerEvent(SignalREventLabels.missionDefinitionDeleted, (username: string, message: string) => {
                const mDef: MissionDefinition = JSON.parse(message)
                if (!mDef.area) return
                setMissionDefinitions((oldMissionDefs) => {
                    const oldListCopy = [...oldMissionDefs]
                    const queueIndex = oldListCopy.findIndex((m) => m.id === mDef.id)
                    if (queueIndex !== -1) oldListCopy.splice(queueIndex, 1) // Remove deleted mission definition
                    return oldListCopy
                })
            })
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [registerEvent, connectionReady])

    useEffect(() => {
        const fetchAndUpdateMissionDefinitions = async () => {
            const missionDefinitionsInInstallation = await fetchMissionDefinitions({
                installationCode: installationCode,
                pageSize: 100,
                orderBy: 'InstallationCode installationCode',
            }).catch((e) => {
                setAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertContent
                        translatedMessage={TranslateText('Failed to retrieve inspection plans')}
                    />,
                    AlertCategory.ERROR
                )
                setListAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertListContent
                        translatedMessage={TranslateText('Failed to retrieve inspection plans')}
                    />,
                    AlertCategory.ERROR
                )
            })
            setMissionDefinitions(missionDefinitionsInInstallation ?? [])
        }
        if (BackendAPICaller.accessToken) fetchAndUpdateMissionDefinitions()
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [BackendAPICaller.accessToken, installationCode])

    const [filteredMissionDefinitions, setFilteredMissionDefinitions] = useState<MissionDefinition[]>([])

    useEffect(() => {
        setFilteredMissionDefinitions(
            missionDefinitions.filter((m) => m.installationCode.toLowerCase() === installationCode.toLowerCase())
        )
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [installationCode, missionDefinitions])

    return { missionDefinitions: filteredMissionDefinitions }
}

export const MissionDefinitionsProvider: FC<Props> = ({ children }) => {
    const { missionDefinitions } = useMissionDefinitions()
    return (
        <MissionDefinitionsContext.Provider value={{ missionDefinitions }}>
            {children}
        </MissionDefinitionsContext.Provider>
    )
}

export const useMissionDefinitionsContext = () => useContext(MissionDefinitionsContext)
