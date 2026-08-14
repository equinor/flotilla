import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'

const WIDTH = 360
const HEIGHT = 190
const TOP_PAD = 28
const BOTTOM_PAD = 28
const BASELINE = HEIGHT - BOTTOM_PAD
const BAR_MAX_HEIGHT = BASELINE - TOP_PAD
const MAX_BAR_WIDTH = 56

const StyledSvg = styled.svg`
    display: block;
    width: 100%;
    height: auto;
    font-family: Equinor, sans-serif;
`

interface WeeklyBar {
    label: string
    value: number
}

interface WeeklyBarChartProps {
    data: WeeklyBar[]
}

export const WeeklyBarChart = ({ data }: WeeklyBarChartProps) => {
    if (data.length === 0) return null

    const maxValue = Math.max(...data.map((bar) => bar.value), 1)
    const average = data.reduce((sum, bar) => sum + bar.value, 0) / data.length
    const averageY = BASELINE - (average / maxValue) * BAR_MAX_HEIGHT
    const slotWidth = WIDTH / data.length
    const barWidth = Math.min(slotWidth * 0.5, MAX_BAR_WIDTH)

    return (
        <StyledSvg viewBox={`0 0 ${WIDTH} ${HEIGHT}`} role="img" aria-label="Completed missions per week">
            <line
                x1={0}
                y1={BASELINE}
                x2={WIDTH}
                y2={BASELINE}
                stroke={tokens.colors.ui.background__medium.hex}
                strokeWidth={1}
            />
            <line
                x1={0}
                y1={averageY}
                x2={WIDTH}
                y2={averageY}
                stroke={tokens.colors.interactive.warning__resting.hex}
                strokeWidth={1.2}
                strokeDasharray="4 4"
            />
            {data.map((bar, index) => {
                const centerX = index * slotWidth + slotWidth / 2
                const barHeight = (bar.value / maxValue) * BAR_MAX_HEIGHT
                const barY = BASELINE - barHeight
                return (
                    <g key={bar.label}>
                        <rect
                            x={centerX - barWidth / 2}
                            y={barY}
                            width={barWidth}
                            height={barHeight}
                            rx={3}
                            fill={tokens.colors.interactive.primary__resting.hex}
                        />
                        <text
                            x={centerX}
                            y={barY - 8}
                            textAnchor="middle"
                            fontSize="13"
                            fontWeight="600"
                            fill={tokens.colors.text.static_icons__default.hex}
                        >
                            {bar.value}
                        </text>
                        <text
                            x={centerX}
                            y={BASELINE + 18}
                            textAnchor="middle"
                            fontSize="10"
                            fill={tokens.colors.text.static_icons__tertiary.hex}
                        >
                            {bar.label}
                        </text>
                    </g>
                )
            })}
        </StyledSvg>
    )
}
