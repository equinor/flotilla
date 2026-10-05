import { useQuery } from '@tanstack/react-query'
import { useSaraApi } from 'api/UseSaraApi'
import { AnalysisValueDisplay } from 'components/Displays/TaskDisplay'

export const MeasurementDisplay = ({ recordId, isCompact = true }: { recordId: string; isCompact?: boolean }) => {
    const saraApi = useSaraApi()
    const { data } = useQuery({
        queryKey: ['inspectionMeasurement', recordId],
        queryFn: () => saraApi.getMeasurement(recordId),
        staleTime: 10 * 60 * 1000,
        retry: 1,
    })

    return data ? <AnalysisValueDisplay value={data.value.toFixed(4)} unit={data.unit} isCompact={isCompact} /> : null
}
