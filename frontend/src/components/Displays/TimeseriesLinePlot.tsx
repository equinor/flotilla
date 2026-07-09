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
    Plugin,
} from 'chart.js'
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

export interface TimeseriesLinePlotDataPoint {
    time: Date
    value: number
    inspectionId: string
}

export interface TimeseriesLinePlotData {
    [inspectionId: string]: TimeseriesLinePlotDataPoint[]
}

interface Props {
    data: TimeseriesLinePlotData
    yLabel: string
    ymin: number
    ymax: number
    onPointClick?: (point: TimeseriesLinePlotDataPoint) => void
    selectedInspectionId: string | undefined
}

export const TimeseriesLinePlot = ({ data, yLabel, ymin, ymax, onPointClick, selectedInspectionId }: Props) => {
    const dataValues = useMemo(() => Object.values(data), [data])

    const options: ChartOptions<'line'> = useMemo(
        () => ({
            responsive: true,
            spanGaps: true,
            onClick: (_event, elements) => {
                if (!onPointClick || elements.length === 0) return
                const { datasetIndex, index } = elements[0]
                const entry = dataValues[datasetIndex]
                if (!entry) return

                const point = entry[index]
                if (!point) return
                onPointClick(point)
            },
            onHover: (event, elements) => {
                const target = event.native?.target as HTMLElement | undefined
                if (target) target.style.cursor = onPointClick && elements.length > 0 ? 'pointer' : 'default'
            },
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
                        unit: 'day',
                        displayFormats: {
                            millisecond: 'dd.MM',
                            second: 'dd.MM',
                            minute: 'dd.MM',
                            hour: 'dd.MM',
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
        [yLabel, ymin, ymax, dataValues, onPointClick]
    )

    // @ts-expect-error ; Date isn't assignable to number for x-axis value - assumption: library can handle it anyways
    const datasets: ChartDataset<'line', DefaultDataPoint<'line'>>[] = Object.entries(data).map(
        ([inspectionId, dataPointArray], index: number) => {
            const [borderColor, backgroundColor] = PALETTE[index % PALETTE.length]
            const isSelected = (pointIndex: number) =>
                selectedInspectionId !== undefined && dataPointArray[pointIndex].inspectionId === selectedInspectionId
            return {
                label: inspectionId,
                data: dataPointArray.map((dataPoint) => ({
                    x: dataPoint.time,
                    y: dataPoint.value,
                })),
                borderColor,
                backgroundColor,
                pointRadius: (ctx) => (isSelected(ctx.dataIndex) ? 8 : 4),
                pointHoverRadius: (ctx) => (isSelected(ctx.dataIndex) ? 9 : 6),
                pointBorderWidth: (ctx) => (isSelected(ctx.dataIndex) ? 3 : 1),
                pointBorderColor: (ctx) =>
                    isSelected(ctx.dataIndex) ? tokens.colors.text.static_icons__default.hex : borderColor,
            }
        }
    )

    const chartData: ChartData<'line'> = { labels: [], datasets }
    const plotAreaBackground: Plugin<'line'> = {
        id: 'plotAreaBackground',
        beforeDraw: ({ ctx, chartArea: a }) => {
            ctx.fillStyle = tokens.colors.ui.background__default.hex
            ctx.fillRect(a.left, a.top, a.width, a.height)
        },
    }
    return <Line options={options} data={chartData} plugins={[plotAreaBackground]} />
}
