import { createContext, FC, useContext, useEffect, useMemo, useState } from 'react'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { unsubscribeAll } from 'utils/signalR'
import { MissionDefinition } from 'models/MissionDefinition'
import { useLanguageContext } from './LanguageContext'
import { useAlertContext } from './AlertContext'
import { AlertType, AlertKind } from 'models/Alert'
import { useBackendApi } from 'api/UseBackendApi'
import { InstallationContext } from './InstallationContext'
import { useOnPageVisible } from 'hooks/usePageVisibility'

interface IMissionDefinitionsContext {
    missionDefinitions: MissionDefinition[]
}

interface Props {
    children: React.ReactNode
}

const defaultMissionDefinitionsContext: IMissionDefinitionsContext = {
    missionDefinitions: [],
}

const MissionDefinitionsContext = createContext<IMissionDefinitionsContext>(defaultMissionDefinitionsContext)

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

const useMissionDefinitions = (): IMissionDefinitionsContext => {
    const [missionDefinitions, setMissionDefinitions] = useState<MissionDefinition[]>([])
    const { registerEvent, connectionReady } = useSignalRContext()
    const { installation } = useContext(InstallationContext)
    const { TranslateText } = useLanguageContext()
    const { raiseAlert } = useAlertContext()
    const backendApi = useBackendApi()

    useEffect(() => {
        if (!connectionReady) return
        return unsubscribeAll([
            registerEvent(SignalREventLabels.missionDefinitionUpdated, (username: string, message: string) => {
                const missionDefinition: MissionDefinition = JSON.parse(message)
                setMissionDefinitions((oldMissionDefinitions) =>
                    upsertMissionDefinition(oldMissionDefinitions, missionDefinition)
                )
            }),
            registerEvent(SignalREventLabels.missionDefinitionCreated, (username: string, message: string) => {
                const missionDefinition: MissionDefinition = JSON.parse(message)
                setMissionDefinitions((oldMissionDefinitions) =>
                    upsertMissionDefinition(oldMissionDefinitions, missionDefinition)
                )
            }),
            registerEvent(SignalREventLabels.missionDefinitionDeleted, (username: string, message: string) => {
                const mDef: MissionDefinition = JSON.parse(message)
                setMissionDefinitions((oldMissionDefs) => {
                    const oldListCopy = [...oldMissionDefs]
                    const queueIndex = oldListCopy.findIndex((m) => m.id === mDef.id)
                    if (queueIndex !== -1) oldListCopy.splice(queueIndex, 1) // Remove deleted mission definition
                    return oldListCopy
                })
            }),
        ])
    }, [registerEvent, connectionReady])

    const fetchAndUpdateMissionDefinitions = () => {
        backendApi
            .getMissionDefinitions({
                installationCode: installation.installationCode,
                pageSize: 100,
                orderBy: 'InstallationCode installationCode',
            })
            .then((response) => {
                const missionDefinitionsInInstallation = response.content
                setMissionDefinitions(missionDefinitionsInInstallation ?? [])
            })
            .catch(() => {
                raiseAlert(AlertType.RequestFail, {
                    kind: AlertKind.RequestFail,
                    message: TranslateText('Failed to retrieve inspection plans'),
                })
            })
    }

    useEffect(() => {
        fetchAndUpdateMissionDefinitions()
    }, [installation])

    useOnPageVisible(() => {
        fetchAndUpdateMissionDefinitions()
    })

    const filteredMissionDefinitions = useMemo(
        () =>
            missionDefinitions.filter(
                (m) => m.installationCode.toLowerCase() === installation.installationCode.toLowerCase()
            ),
        [missionDefinitions, installation.installationCode]
    )

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
