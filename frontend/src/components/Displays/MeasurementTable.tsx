import { Table, Typography } from '@equinor/eds-core-react'
import { useQueries } from '@tanstack/react-query'
import { useSaraApi } from 'api/UseSaraApi'
import { useLanguageContext } from 'contexts/LanguageContext'
import { TaskMeasurement } from 'models/InspectionRecord'

const measurementTypeLabel = (inspectionType: string) => inspectionType.replace(/Measurement$/, '')

export const MeasurementTable = ({ measurements }: { measurements: TaskMeasurement[] }) => {
    const { TranslateText } = useLanguageContext()
    const saraApi = useSaraApi()
    const results = useQueries({
        queries: measurements.map((measurement) => ({
            queryKey: ['inspectionMeasurement', measurement.recordId],
            queryFn: () => saraApi.getMeasurement(measurement.recordId),
            staleTime: 10 * 60 * 1000,
            retry: 1,
        })),
    })

    const isPending = results.some((result) => result.isPending)
    const rows = measurements
        .map((measurement, index) => ({ measurement, data: results[index].data }))
        .sort((left, right) => {
            if (left.data && right.data) return left.data.unit.localeCompare(right.data.unit)
            return Number(!left.data) - Number(!right.data)
        })

    return (
        <Table>
            <Table.Head>
                <Table.Row>
                    <Table.Cell>{TranslateText('Type')}</Table.Cell>
                    <Table.Cell>{TranslateText('Value')}</Table.Cell>
                    <Table.Cell>{TranslateText('Unit')}</Table.Cell>
                </Table.Row>
            </Table.Head>
            <Table.Body>
                {isPending ? (
                    <Table.Row>
                        <Table.Cell colSpan={3}>—</Table.Cell>
                    </Table.Row>
                ) : (
                    rows.map(({ measurement, data }) => (
                        <Table.Row key={measurement.recordId}>
                            <Table.Cell>{measurementTypeLabel(measurement.inspectionType)}</Table.Cell>
                            {data ? (
                                <>
                                    <Table.Cell>{data.value.toFixed(4)}</Table.Cell>
                                    <Table.Cell>{data.unit}</Table.Cell>
                                </>
                            ) : (
                                <Table.Cell colSpan={2}>
                                    <Typography variant="caption">
                                        {TranslateText('Measurement is unavailable')}
                                    </Typography>
                                </Table.Cell>
                            )}
                        </Table.Row>
                    ))
                )}
            </Table.Body>
        </Table>
    )
}
