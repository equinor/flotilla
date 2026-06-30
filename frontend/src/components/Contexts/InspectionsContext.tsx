import { createContext, FC, useContext, useEffect, useRef } from 'react'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { SaraAnalysisResultReady, SaraInspectionVisualizationReady } from 'models/Inspection'
import { useQuery } from '@tanstack/react-query'
import { queryClient } from '../../App'
import { useBackendApi } from 'api/UseBackendApi'

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
interface IExistsData {
    exists: boolean
    isPending: boolean
    isError: boolean
}
interface IInspectionsContext {
    useMediaData: (inspectionId: string) => IImageData
    useMediaExists: (inspectionId: string) => IExistsData
    useAnalysisData: (inspectionId: string) => IImageData
    useValueData: (inspectionId: string) => IValueData
}

interface Props {
    children: React.ReactNode
}

const defaultInspectionsContext = {
    useMediaData: () => ({ data: undefined, isPending: false, isError: true }),
    useMediaExists: () => ({ exists: false, isPending: false, isError: true }),
    useAnalysisData: () => ({ data: undefined, isPending: false, isError: true }),
    useValueData: () => ({ data: undefined, isPending: false, isError: true }),
}

const InspectionsContext = createContext<IInspectionsContext>(defaultInspectionsContext)

export const InspectionsProvider: FC<Props> = ({ children }) => {
    const { registerEvent, connectionReady } = useSignalRContext()
    const backendApi = useBackendApi()

    // Keep a stable ref to backendApi so callbacks don't capture a stale closure
    const backendApiRef = useRef(backendApi)
    useEffect(() => {
        backendApiRef.current = backendApi
    }, [backendApi])

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.inspectionVisualizationReady, (username: string, message: string) => {
                const inspectionVisualizationData: SaraInspectionVisualizationReady = JSON.parse(message)
                const inspectionId = inspectionVisualizationData.inspectionId
                queryClient.invalidateQueries({
                    queryKey: ['fetchInspectionData', inspectionId],
                })
                queryClient.invalidateQueries({
                    queryKey: ['inspectionMediaExists', inspectionId],
                })
                queryClient.fetchQuery({
                    queryKey: ['fetchInspectionData', inspectionId],
                    queryFn: async () => {
                        const mediaBlob = await backendApiRef.current.getInspectionMedia(inspectionId)
                        return URL.createObjectURL(mediaBlob)
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
                        const imageBlob = await backendApiRef.current.getAnalysis(inspectionId)
                        return URL.createObjectURL(imageBlob)
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
                const mediaBlob = await backendApi.getInspectionMedia(inspectionId)
                return URL.createObjectURL(mediaBlob)
            },
            retry: 1,
            staleTime: 10 * 60 * 1000, // If data is received, stale time is 10 min before making new API call
            enabled: inspectionId !== undefined,
        })

        return { data: result.data, isPending: result.isPending, isError: result.isError }
    }

    const useMediaExists = (inspectionId: string): IExistsData => {
        const result = useQuery({
            queryKey: ['inspectionMediaExists', inspectionId],
            queryFn: async () => backendApi.getInspectionMediaExists(inspectionId),
            retry: 1,
            staleTime: 10 * 60 * 1000, // If data is received, stale time is 10 min before making new API call
            enabled: inspectionId !== undefined,
        })

        return { exists: result.data ?? false, isPending: result.isPending, isError: result.isError }
    }

    const useAnalysisData = (inspectionId: string): IImageData => {
        const result = useQuery({
            queryKey: ['fetchAnalysisData', inspectionId],
            queryFn: async () => {
                const imageBlob = await backendApi.getAnalysis(inspectionId)
                return URL.createObjectURL(imageBlob)
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
                useMediaExists,
                useAnalysisData,
                useValueData,
            }}
        >
            {children}
        </InspectionsContext.Provider>
    )
}

export const useInspectionsContext = () => useContext(InspectionsContext)
