import { createContext, FC, useContext, useEffect, useRef } from 'react'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { SaraAnalysisResultReady, SaraInspectionVisualizationReady } from 'models/Inspection'
import { useQuery } from '@tanstack/react-query'
import { queryClient } from '../../App'
import { useBackendApi } from 'api/UseBackendApi'
import { useSaraApi } from 'api/UseSaraApi'

interface IImageData {
    data: string | undefined
    isPending: boolean
    isError: boolean
}
interface IValueData {
    data: number | undefined
    isPending: boolean
    isError: boolean
}
interface IInspectionsContext {
    useMediaData: (inspectionId: string) => IImageData
    useAnalysisData: (inspectionId: string) => IImageData
    useValueData: (inspectionId: string) => IValueData
}

interface Props {
    children: React.ReactNode
}

const defaultInspectionsContext = {
    useMediaData: () => ({ data: undefined, isPending: false, isError: true }),
    useAnalysisData: () => ({ data: undefined, isPending: false, isError: true }),
    useValueData: () => ({ data: undefined, isPending: false, isError: true }),
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

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.inspectionVisualizationReady, (username: string, message: string) => {
                const inspectionVisualizationData: SaraInspectionVisualizationReady = JSON.parse(message)
                const inspectionId = inspectionVisualizationData.inspectionId
                queryClient.invalidateQueries({
                    queryKey: ['fetchInspectionData', inspectionId],
                })
                queryClient.fetchQuery({
                    queryKey: ['fetchInspectionData', inspectionId],
                    queryFn: async () => {
                        return await saraApiRef.current.getAnonymizedDataUrl(inspectionId)
                    },
                    retry: 1,
                    staleTime: 10 * 60 * 1000,
                })
            })
        }
    }, [registerEvent, connectionReady])

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.analysisResultReady, (username: string, message: string) => {
                const analysisResultData: SaraAnalysisResultReady = JSON.parse(message)
                const inspectionId = analysisResultData.inspectionId
                queryClient.invalidateQueries({
                    queryKey: ['fetchAnalysisData', inspectionId],
                })
                queryClient.fetchQuery({
                    queryKey: ['fetchAnalysisData', inspectionId],
                    queryFn: async () => {
                        return await saraApiRef.current.getAnalysedDataUrl(inspectionId)
                    },
                    retry: 1,
                    staleTime: 10 * 60 * 1000,
                })
            })
        }
    }, [registerEvent, connectionReady])

    const useMediaData = (inspectionId: string): IImageData => {
        const result = useQuery({
            queryKey: ['fetchInspectionData', inspectionId],
            queryFn: async () => {
                return await saraApiRef.current.getAnonymizedDataUrl(inspectionId)
            },
            retry: 1,
            staleTime: 10 * 60 * 1000, // If data is received, stale time is 10 min before making new API call
            enabled: inspectionId !== undefined,
        })

        return { data: result.data, isPending: result.isPending, isError: result.isError }
    }

    const useAnalysisData = (inspectionId: string): IImageData => {
        const result = useQuery({
            queryKey: ['fetchAnalysisData', inspectionId],
            queryFn: async () => {
                return await saraApiRef.current.getAnalysedDataUrl(inspectionId)
            },
            retry: 1,
            staleTime: 10 * 60 * 1000, // If data is received, stale time is 10 min before making new API call
            enabled: inspectionId !== undefined,
        })
        return { data: result.data, isPending: result.isPending, isError: result.isError }
    }

    const useValueData = (inspectionId: string): IValueData => {
        const result = useQuery({
            queryKey: ['fetchValueData', inspectionId],
            queryFn: async () => {
                const value = await backendApi.getValue(inspectionId)
                return value
            },
            retry: 1,
            staleTime: 10 * 60 * 1000, // If data is received, stale time is 10 min before making new API call
            enabled: inspectionId !== undefined,
        })
        return { data: result.data, isPending: result.isPending, isError: result.isError }
    }

    return (
        <InspectionsContext.Provider
            value={{
                useMediaData,
                useAnalysisData,
                useValueData,
            }}
        >
            {children}
        </InspectionsContext.Provider>
    )
}

export const useInspectionsContext = () => useContext(InspectionsContext)
