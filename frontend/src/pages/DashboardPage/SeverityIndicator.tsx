import { Icon, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { useLanguageContext } from 'contexts/LanguageContext'
import { AnalysisSeverity } from 'models/analysis/AnalysisSeverity'
import { Icons } from 'utils/icons'

interface SeverityStyle {
    icon: Icons
    color: string
    backgroundColor: string
    label: string
}

/**
 * Single mapping from severity to presentation, so tables, chips and any future
 * panel stay consistent without repeating the switch.
 */
export const severityStyles: Record<AnalysisSeverity, SeverityStyle> = {
    [AnalysisSeverity.Alarm]: {
        icon: Icons.Failed,
        color: tokens.colors.interactive.danger__resting.hex,
        backgroundColor: tokens.colors.interactive.danger__highlight.hex,
        label: 'Alarm',
    },
    [AnalysisSeverity.Warning]: {
        icon: Icons.Warning,
        color: tokens.colors.interactive.warning__resting.hex,
        backgroundColor: tokens.colors.interactive.warning__highlight.hex,
        label: 'Warning',
    },
    [AnalysisSeverity.Normal]: {
        icon: Icons.Successful,
        color: tokens.colors.interactive.success__resting.hex,
        backgroundColor: tokens.colors.ui.background__default.hex,
        label: 'Normal',
    },
    [AnalysisSeverity.Unknown]: {
        icon: Icons.Info,
        color: tokens.colors.text.static_icons__tertiary.hex,
        backgroundColor: tokens.colors.ui.background__default.hex,
        label: 'Unknown',
    },
}

const SeverityRow = styled.span`
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    white-space: nowrap;
`

export const SeverityIndicator = ({ severity }: { severity: AnalysisSeverity }) => {
    const { TranslateText } = useLanguageContext()
    const style = severityStyles[severity]

    return (
        <SeverityRow>
            <Icon name={style.icon} size={18} color={style.color} />
            <Typography variant="body_short">{TranslateText(style.label)}</Typography>
        </SeverityRow>
    )
}

const CountChip = styled.span<{ $color: string }>`
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-width: 28px;
    height: 24px;
    padding: 0 0.5rem;
    border-radius: 12px;
    background: ${({ $color }) => $color};
    color: ${tokens.colors.text.static_icons__primary_white.hex};
    font-weight: 700;
    font-size: 0.8rem;
`

export const SeverityCountChip = ({ severity, count }: { severity: AnalysisSeverity; count: number }) => (
    <CountChip $color={severityStyles[severity].color}>{count}</CountChip>
)
