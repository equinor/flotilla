import { Button, Typography } from '@equinor/eds-core-react'
import { useState } from 'react'
import { Task } from 'models/Task'
import { AnalysisResultDialogView } from '../AnalysisResultView'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import styled from 'styled-components'

const Styledbutton = styled(Button)`
    &:hover {
        variant: outlined;
        border-color: red;
    }
`

export const TaskAnalysisDisplay = ({ task }: { task: Task }) => {
    const [dialogOpen, setDialogOpen] = useState(false)
    const { TranslateText } = useLanguageContext()

    const analysis = task.inspection.analysisResult

    const handleOpenDialog = () => setDialogOpen(true)
    const handleCloseDialog = () => setDialogOpen(false)

    return (
        <>
            {analysis?.analysisType && (
                <Styledbutton variant="ghost" color="danger" onClick={handleOpenDialog}>
                    <Typography link color="danger">
                        {TranslateText('Result')}
                    </Typography>
                </Styledbutton>
            )}
            {dialogOpen && <AnalysisResultDialogView selectedTask={task} onClose={handleCloseDialog} />}
        </>
    )
}
