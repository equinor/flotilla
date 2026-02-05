import { Button, Typography } from '@equinor/eds-core-react'
import { Task } from 'models/Task'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import styled from 'styled-components'
import { useInspectionId } from 'pages/InspectionReportPage/SetInspectionIdHook'

const Styledbutton = styled(Button)`
    &:hover {
        variant: outlined;
        border-color: red;
    }
`

export const TaskAnalysisDisplay = ({ task }: { task: Task }) => {
    const { TranslateText } = useLanguageContext()
    const { switchSelectedAnalysisId } = useInspectionId()

    const analysis = task.inspection.analysisResult

    return (
        <>
            {analysis?.analysisType && (
                <Styledbutton
                    variant="ghost"
                    color="danger"
                    onClick={() => switchSelectedAnalysisId(task.inspection.isarInspectionId)}
                >
                    <Typography link color="danger">
                        {TranslateText('Result')}
                    </Typography>
                </Styledbutton>
            )}
        </>
    )
}
