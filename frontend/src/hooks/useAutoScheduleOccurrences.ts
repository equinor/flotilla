import { useCallback, useEffect, useState } from 'react'
import { useBackendApi } from 'api/UseBackendApi'
import { useSignalRContext } from 'contexts/SignalRContext'
import { AutoScheduleOccurrence } from 'models/AutoScheduleOccurrence'
import { MissionDefinition } from 'models/MissionDefinition'
import { useOnPageVisible } from './usePageVisibility'

export const useAutoScheduleOccurrences = (installationCode: string, missionDefinitions: MissionDefinition[]) => {
    const api = useBackendApi()
    const { connectionReady } = useSignalRContext()
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
        return () => {
            active = false
            window.clearInterval(timer)
        }
    }, [api, installationCode, missionDefinitions, connectionReady, revision])

    const current = result?.installationCode === installationCode ? result : undefined
    return {
        autoScheduleOccurrences: current?.occurrences ?? [],
        autoScheduleLoading: !current,
        autoScheduleError: current?.error ?? false,
        refreshOccurrences,
    }
}
