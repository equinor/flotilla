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
import { useMemo } from 'react'
import { Line } from 'react-chartjs-2'
import { tokens } from '@equinor/eds-tokens'
import 'chartjs-adapter-luxon' // registers the Luxon adapter for the x-axis time scale

ChartJS.register(TimeScale, CategoryScale, LinearScale, PointElement, LineElement, Title, Tooltip, Legend)

const hexToRgba = (hex: string, alpha: number): string => {
    const r = parseInt(hex.slice(1, 3), 16)
    const g = parseInt(hex.slice(3, 5), 16)
    const b = parseInt(hex.slice(5, 7), 16)
    return `rgba(${r}, ${g}, ${b}, ${alpha})`
}

const PALETTE_COLORS = [
    tokens.colors.infographic.primary__energy_red_100.hex,
    tokens.colors.infographic.primary__weathered_red.hex,
    tokens.colors.infographic.primary__slate_blue.hex,
    tokens.colors.infographic.primary__moss_green_100.hex,
    tokens.colors.infographic.substitute__purple_berry.hex,
    tokens.colors.infographic.substitute__blue_ocean.hex,
    tokens.colors.infographic.substitute__green_cucumber.hex,
    tokens.colors.infographic.substitute__pink_rose.hex,
]

const PALETTE: [string, string][] = PALETTE_COLORS.map((hex) => [hex, hexToRgba(hex, 0.5)])

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
    ymin: number
    ymax: number
}

export const TimeseriesLinePlot = ({ data, yLabel, ymin, ymax }: Props) => {
    const options: ChartOptions<'line'> = useMemo(
        () => ({
            responsive: true,
            spanGaps: true,

            plugins: {
                legend: {
                    position: 'right' as const,
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
                    min: ymin,
                    max: ymax,
                },
            },
        }),
        [yLabel]
    )

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
                pointRadius: 4,
                pointHoverRadius: 6,
            }
        }
    )

    const chartData: ChartData<'line'> = { labels: [], datasets }
    return <Line options={options} data={chartData} />
}
