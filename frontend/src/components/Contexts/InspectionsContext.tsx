import { createContext, FC, useContext, useEffect, useRef, useCallback } from 'react'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { queryClient } from '../../App'
import { useBackendApi } from 'api/UseBackendApi'
import { useSaraApi } from 'api/UseSaraApi'
import { FlotillaAnalysisResultMessage, FlotillaInspectionResultMessage, InspectionData } from 'models/InspectionRecord'
import { AnalysisType } from 'models/MissionDefinition'

interface IInspectionData {
    data: InspectionData | undefined
    isPending: boolean
    isError: boolean
}
interface IInspectionListData {
    data: InspectionData[] | undefined
    isPending: boolean
    isError: boolean
}
interface IInspectionsContext {
    useSaraData: (inspectionId: string) => IInspectionData
    useSaraListData: (
        inspectionIds?: string[] | null,
        installationCode?: string | null,
        tagId?: string | null,
        analysisType?: AnalysisType | null,
        minDate?: Date | null,
        maxDate?: Date | null
    ) => IInspectionListData
    setFeedback: (inspectionId: string, analysisRunId: string, isCorrect: boolean) => Promise<void>
    removeFeedback: (inspectionId: string, analysisRunId: string) => Promise<void>
}

interface Props {
    children: React.ReactNode
}

const defaultInspectionsContext = {
    useSaraData: () => ({ data: undefined, isPending: false, isError: true }),
    useSaraListData: () => ({ data: undefined, isPending: false, isError: true }),
    setFeedback: async () => {},
    removeFeedback: async () => {},
}

const InspectionsContext = createContext<IInspectionsContext>(defaultInspectionsContext)

export const InspectionsProvider: FC<Props> = ({ children }) => {
    const { registerEvent, connectionReady } = useSignalRContext()
    const backendApi = useBackendApi()
    const saraApi = useSaraApi()

    // Keep a stable ref to backendApi so callbacks don't capture a stale closure
    const backendApiRef = useRef(backendApi)
    const saraApiRef = useRef(saraApi)
    useEffect(() => {
        backendApiRef.current = backendApi
        saraApiRef.current = saraApi
    }, [backendApi, saraApi])

    // Pulls the record for one inspection into the cache. Rejections are absorbed
    // on purpose: SARA answers 404 until the analysis exists, and an uncaught
    // rejection here becomes a console error on every SignalR event. Components
    // observing the key still surface the error state through useQuery.
    const refreshInspectionData = useCallback(
        (inspectionId: string) => {
            queryClient
                .fetchQuery({
                    queryKey: ['fetchInspectionData', inspectionId],
                    queryFn: async () => {
                        return await saraApiRef.current.getSaraDataByInspectionId(inspectionId)
                    },
                    retry: 1,
                    staleTime: 10 * 60 * 1000,
                })
                .catch(() => {})
        },
        // queryClient is a module singleton and saraApiRef is a ref, so this
        // callback is stable and safe to list in the effects below.
        []
    )

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.inspectionVisualizationReady, (username: string, message: string) => {
                const { inspectionId }: FlotillaInspectionResultMessage = JSON.parse(message)
                // The mission page reads inspections through the list query with a null
                // installation code and analysis type, so invalidate the whole prefix rather
                // than trying to reconstruct a key that would not match.
                queryClient.invalidateQueries({
                    queryKey: ['fetchInspectionListData'],
                })
                // Not invalidated first: invalidateQueries refetches the key, then
                // fetchQuery cancels that in-flight refetch (it defaults to
                // cancelRefetch), and the rejected promise surfaces as an unhandled
                // CancelledError. fetchQuery alone refreshes the key and also
                // populates it when no component is observing.
                refreshInspectionData(inspectionId)
            })
        }
    }, [registerEvent, connectionReady, refreshInspectionData])

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.analysisResultReady, (username: string, message: string) => {
                const inspectionResult: FlotillaAnalysisResultMessage = JSON.parse(message)
                // The mission page passes null for both installation code and analysis type,
                // so a prefixed key never matches it. Invalidate every list query instead.
                queryClient.invalidateQueries({
                    queryKey: ['fetchInspectionListData'],
                })
                refreshInspectionData(inspectionResult.inspectionId)
            })
        }
    }, [registerEvent, connectionReady, refreshInspectionData])

    const useSaraData = (inspectionId: string): IInspectionData => {
        const result = useQuery({
            queryKey: ['fetchInspectionData', inspectionId],
            queryFn: async () => {
                return await saraApiRef.current.getSaraDataByInspectionId(inspectionId)
            },
            retry: 1,
            staleTime: 10 * 60 * 1000, // If data is received, stale time is 10 min before making new API call
            enabled: inspectionId !== undefined,
        })
        return { data: result.data, isPending: result.isPending, isError: result.isError }
    }

    const useSaraListData = (
        inspectionIds?: string[] | null,
        installationCode?: string | null,
        tagId?: string | null,
        analysisType?: AnalysisType | null,
        minDate?: Date | null,
        maxDate?: Date | null
    ): IInspectionListData => {
        const result = useQuery({
            queryKey: [
                'fetchInspectionListData',
                installationCode,
                analysisType,
                {
                    inspectionIds,
                    tagId,
                    minDate: minDate?.toISOString(),
                    maxDate: maxDate?.toISOString(),
                },
            ],
            queryFn: async () => {
                return await saraApiRef.current.getSaraData(
                    inspectionIds,
                    installationCode,
                    tagId,
                    analysisType,
                    minDate,
                    maxDate
                )
            },
            retry: 1,
            staleTime: 10 * 60 * 1000, // If data is received, stale time is 10 min before making new API call
            // Changing the time range changes the query key, so keep the previous series on screen
            // instead of tearing the page down to a loading state and losing the current selection.
            placeholderData: keepPreviousData,
        })
        return { data: result.data, isPending: result.isPending, isError: result.isError }
    }

    // Feedback is shared by all users, so refetch afterwards to pick up whatever is now
    // stored rather than assuming our own write won. Both the single-inspection query and
    // the list queries carry feedback, and the analysis dialog is fed by the list.
    const refetchInspection = async (inspectionId: string) => {
        await queryClient.invalidateQueries({
            queryKey: ['fetchInspectionData', inspectionId],
        })
        await queryClient.invalidateQueries({
            queryKey: ['fetchInspectionListData'],
        })
    }

    const setFeedback = async (inspectionId: string, analysisRunId: string, isCorrect: boolean) => {
        await saraApiRef.current.upsertFeedback(analysisRunId, isCorrect)
        await refetchInspection(inspectionId)
    }

    const removeFeedback = async (inspectionId: string, analysisRunId: string) => {
        await saraApiRef.current.deleteFeedback(analysisRunId)
        await refetchInspection(inspectionId)
    }

    return (
        <InspectionsContext.Provider
            value={{
                useSaraData,
                useSaraListData,
                setFeedback,
                removeFeedback,
            }}
        >
            {children}
        </InspectionsContext.Provider>
    )
}

export const useInspectionsContext = () => useContext(InspectionsContext)
