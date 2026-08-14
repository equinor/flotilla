import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'

const RADIUS = 52
const STROKE = 16
const SIZE = 140
const CENTER = SIZE / 2
const CIRCUMFERENCE = 2 * Math.PI * RADIUS

const StyledSvg = styled.svg`
    display: block;
    flex-shrink: 0;
    font-family: Equinor, sans-serif;
`

interface DonutChartProps {
    fraction: number
    color: string
    caption: string
}

export const DonutChart = ({ fraction, color, caption }: DonutChartProps) => {
    const clamped = Math.min(Math.max(fraction, 0), 1)
    const percentage = Math.round(clamped * 100)
    const dashArray = `${clamped * CIRCUMFERENCE} ${CIRCUMFERENCE}`

    return (
        <StyledSvg
            width={SIZE}
            height={SIZE}
            viewBox={`0 0 ${SIZE} ${SIZE}`}
            role="img"
            aria-label={`${percentage}% ${caption}`}
        >
            <circle
                cx={CENTER}
                cy={CENTER}
                r={RADIUS}
                fill="none"
                stroke={tokens.colors.ui.background__medium.hex}
                strokeWidth={STROKE}
            />
            <circle
                cx={CENTER}
                cy={CENTER}
                r={RADIUS}
                fill="none"
                stroke={color}
                strokeWidth={STROKE}
                strokeLinecap="round"
                strokeDasharray={dashArray}
                transform={`rotate(-90 ${CENTER} ${CENTER})`}
            />
            <text
                x={CENTER}
                y={CENTER - 4}
                textAnchor="middle"
                dominantBaseline="middle"
                fontSize="30"
                fontWeight="700"
                fill={tokens.colors.text.static_icons__default.hex}
            >
                {percentage}%
            </text>
            <text
                x={CENTER}
                y={CENTER + 22}
                textAnchor="middle"
                dominantBaseline="middle"
                fontSize="11"
                fill={tokens.colors.text.static_icons__tertiary.hex}
            >
                {caption}
            </text>
        </StyledSvg>
    )
}
