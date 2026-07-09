import { Button, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import styled from 'styled-components'
import { useInspectionId } from 'pages/InspectionReportPage/SetInspectionIdHook'
import { tokens } from '@equinor/eds-tokens'
import { InspectionData } from 'models/InspectionRecord'

const StyledButton = styled(Button)<{ hasFinding: boolean }>`
    &:hover {
        ${({ $hasFinding }) =>
            $hasFinding
                ? `border-color: ${tokens.colors.interactive.danger__hover.hex};`
                : `border-color: ${tokens.colors.interactive.primary__hover.hex};`};
    }
`

export const TaskAnalysisDisplay = ({ inspectionData }: { inspectionData: InspectionData }) => {
    const { TranslateText } = useLanguageContext()
    const { switchSelectedAnalysisId } = useInspectionId()

    return (
        <StyledButton
            hasFinding={inspectionData.warning}
            color={inspectionData.warning ? 'danger' : 'primary'}
            variant="ghost"
            onClick={() => switchSelectedAnalysisId(inspectionData.inspectionId)}
        >
            <Typography link color={inspectionData.warning ? 'danger' : 'primary'}>
                {TranslateText('Result')}
            </Typography>
        </StyledButton>
    )
}
