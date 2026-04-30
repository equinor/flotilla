import {
    Chart as ChartJS,
    CategoryScale,
    LinearScale,
    PointElement,
    LineElement,
    Title,
    TimeScale,
    Tooltip,
    Legend,
    ChartData,
    ChartOptions,
    DefaultDataPoint,
    ChartDataset,
} from 'chart.js'
import { DateTime } from 'luxon'
import { Line } from 'react-chartjs-2'
import 'chartjs-adapter-luxon' // registers the Luxon adapter for the x-axis time scale

ChartJS.register(TimeScale, CategoryScale, LinearScale, PointElement, LineElement, Title, Tooltip, Legend)

const PALETTE: [string, string][] = [
    ['rgb(255, 18, 67)', 'rgba(255, 18, 67, 0.5)'],
    ['rgb(125, 0, 35)', 'rgba(125, 0, 35, 0.5)'],
    ['rgb(36, 55, 70)', 'rgba(36, 55, 70, 0.5)'],
    ['rgb(0, 112, 121)', 'rgba(0, 112, 121, 0.5)'],
]

interface TimeseriesLinePlotDataPoint {
    time: Date
    value: number
}

export interface TimeseriesLinePlotData {
    [id: string]: TimeseriesLinePlotDataPoint[]
}

interface Props {
    data: TimeseriesLinePlotData
    yLabel: string
}

export const TimeseriesLinePlot = ({ data, yLabel }: Props) => {
    const options: ChartOptions<'line'> = {
        responsive: true,
        spanGaps: true,

        plugins: {
            legend: {
                position: 'right' as const,
            },
            title: {
                display: false,
            },
        },
        scales: {
            x: {
                type: 'time',
                adapters: {
                    date: {
                        zone: 'Europe/Oslo',
                    },
                },
                time: {
                    // Luxon format tokens (https://moment.github.io/luxon/#/formatting?id=table-of-tokens)
                    tooltipFormat: 'dd.MM.yy HH:mm',
                    displayFormats: {
                        millisecond: 'HH:mm:ss.SSS',
                        second: 'HH:mm:ss',
                        minute: 'HH:mm',
                        hour: 'HH:mm',
                        day: 'dd.MM',
                        week: 'dd.MM',
                        month: 'MM.yyyy',
                        quarter: 'qq yyyy',
                        year: 'yyyy',
                    },
                },
                title: {
                    display: true,
                    text: 'Time',
                },
            },
            y: {
                title: {
                    display: true,
                    text: yLabel,
                },
            },
        },
    }

    const labels: string[] = []
    // @ts-expect-error ; Date isn't assignable to number for x-axis value - assumption: library can handle it anyways
    const datasets: ChartDataset<'line', DefaultDataPoint<'line'>>[] = Object.entries(data).map(
        ([id, dataPointArray], index: number) => {
            const [borderColor, backgroundColor] = PALETTE[index % PALETTE.length]
            return {
                label: id,
                data: dataPointArray.map((dataPoint) => ({
                    x: DateTime.fromJSDate(dataPoint.time).toISO(),
                    y: dataPoint.value,
                })),
                borderColor,
                backgroundColor,
            }
        }
    )

    const chartData: ChartData<'line'> = { labels, datasets }
    return <Line options={options} data={chartData} />
}
