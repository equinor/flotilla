import { Button, Typography } from '@equinor/eds-core-react'
import { Task } from 'models/Task'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import styled from 'styled-components'
import { useInspectionId } from 'pages/InspectionReportPage/SetInspectionIdHook'
import { tokens } from '@equinor/eds-tokens'

const StyledButton = styled(Button)<{ hasFinding: boolean }>`
    &:hover {
        ${({ $hasFinding }) =>
            $hasFinding
                ? `border-color: ${tokens.colors.interactive.danger__hover.hex};`
                : `border-color: ${tokens.colors.interactive.primary__hover.hex};`};
    }
`

export const TaskAnalysisDisplay = ({ task }: { task: Task }) => {
    const { TranslateText } = useLanguageContext()
    const { switchSelectedAnalysisId } = useInspectionId()

    const analysis = task.inspection.analysisResult
    const hasFinding = !!task.inspection.analysisResult?.warning

    return (
        <>
            {analysis?.analysisType && (
                <StyledButton
                    hasFinding={hasFinding}
                    color={hasFinding ? 'danger' : 'primary'}
                    variant="ghost"
                    onClick={() => switchSelectedAnalysisId(task.inspection.isarInspectionId)}
                >
                    <Typography link color={hasFinding ? 'danger' : 'primary'}>
                        {TranslateText('Result')}
                    </Typography>
                </StyledButton>
            )}
        </>
    )
}
