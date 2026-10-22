import { useCallback, useEffect, useState } from 'react'
import { useBackendApi } from 'api/UseBackendApi'
import { SignalREventLabels, useSignalRContext } from 'contexts/SignalRContext'
import { AutoScheduleOccurrence } from 'models/AutoScheduleOccurrence'
import { unsubscribeAll } from 'utils/signalR'
import { useOnPageVisible } from './usePageVisibility'

export const useAutoScheduleOccurrences = (installationCode: string) => {
    const api = useBackendApi()
    const { registerEvent, connectionReady } = useSignalRContext()
    const [revision, setRevision] = useState(0)
    const [result, setResult] = useState<{
        installationCode: string
        occurrences: AutoScheduleOccurrence[]
        error: boolean
    }>()
    const refreshOccurrences = useCallback(() => setRevision((value) => value + 1), [])
    useOnPageVisible(refreshOccurrences)

    useEffect(() => {
        let active = true
        let request = 0
        const refresh = async () => {
            const currentRequest = ++request
            try {
                const occurrences = await api.getAutoScheduleOccurrences(installationCode)
                if (active && currentRequest === request) setResult({ installationCode, occurrences, error: false })
            } catch {
                if (active && currentRequest === request) setResult({ installationCode, occurrences: [], error: true })
            }
        }
        void refresh()
        const timer = window.setInterval(() => void refresh(), 30_000)
        const unsubscribe = unsubscribeAll([
            registerEvent(SignalREventLabels.missionDefinitionUpdated, () => void refresh()),
            registerEvent(SignalREventLabels.missionDefinitionCreated, () => void refresh()),
            registerEvent(SignalREventLabels.missionDefinitionDeleted, () => void refresh()),
        ])
        return () => {
            active = false
            window.clearInterval(timer)
            unsubscribe()
        }
    }, [api, installationCode, registerEvent, connectionReady, revision])

    const current = result?.installationCode === installationCode ? result : undefined
    return {
        autoScheduleOccurrences: current?.occurrences ?? [],
        autoScheduleLoading: !current,
        autoScheduleError: current?.error ?? false,
        refreshOccurrences,
    }
}
