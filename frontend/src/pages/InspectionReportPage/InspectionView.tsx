import { Icon, Typography } from '@equinor/eds-core-react'
import { Task } from 'models/Task'
import { Icons } from 'utils/icons'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { formatDateTime } from 'utils/StringFormatting'
import { useInspectionsContext } from 'components/Contexts/InspectionsContext'
import {
    HiddenOnSmallScreen,
    StyledBottomContent,
    StyledCloseButton,
    StyledDialog,
    StyledDialogContent,
    StyledDialogHeader,
    StyledDialogInspectionView,
    StyledInfoContent,
} from './InspectionStyles'
import { InspectionOverviewDialogView } from './InspectionOverview'
import { useState } from 'react'
import { LargeDialogInspectionImage } from './InspectionReportImage'
import { useAssetContext } from 'components/Contexts/AssetContext'

interface InspectionDialogViewProps {
    selectedTask: Task
    tasks: Task[]
}

export const InspectionDialogView = ({ selectedTask, tasks }: InspectionDialogViewProps) => {
    const { TranslateText } = useLanguageContext()
    const { installationName } = useAssetContext()
    const { switchSelectedInspectionTask } = useInspectionsContext()
    const [switchImageDirection, setSwitchImageDirection] = useState<number>(0)

    const successfullyCompletedTasks = tasks.filter((task) => task.status === 'Successful')

    const closeDialog = () => {
        switchSelectedInspectionTask(undefined)
    }

    document.addEventListener('keydown', (event) => {
        if (event.code === 'ArrowLeft' && switchImageDirection !== -1) {
            setSwitchImageDirection(-1)
        } else if (event.code === 'ArrowRight' && switchImageDirection !== 1) {
            setSwitchImageDirection(1)
        }
    })

    document.addEventListener('keyup', (event) => {
        if (
            (event.code === 'ArrowLeft' && switchImageDirection === -1) ||
            (event.code === 'ArrowRight' && switchImageDirection === 1)
        ) {
            const nextTask = successfullyCompletedTasks.indexOf(selectedTask) + switchImageDirection
            if (nextTask >= 0 && nextTask < successfullyCompletedTasks.length) {
                switchSelectedInspectionTask(successfullyCompletedTasks[nextTask])
            }
            setSwitchImageDirection(0)
        }
    })

    return (
        <StyledDialog open={true} isDismissable onClose={closeDialog}>
            <StyledDialogContent>
                <StyledDialogHeader>
                    <Typography variant="accordion_header" group="ui">
                        {TranslateText('Inspection report for task') + ' ' + (selectedTask.taskOrder + 1)}
                    </Typography>
                    <StyledCloseButton variant="ghost" onClick={closeDialog}>
                        <Icon name={Icons.Clear} size={24} />
                    </StyledCloseButton>
                </StyledDialogHeader>
                <StyledDialogInspectionView>
                    <div>
                        <LargeDialogInspectionImage task={selectedTask} />
                        <StyledBottomContent>
                            <StyledInfoContent>
                                <Typography variant="caption">{TranslateText('Installation') + ':'}</Typography>
                                <Typography variant="body_short">{installationName}</Typography>
                            </StyledInfoContent>
                            <StyledInfoContent>
                                <Typography variant="caption">{TranslateText('Tag') + ':'}</Typography>
                                <Typography variant="body_short">{selectedTask.tagId}</Typography>
                            </StyledInfoContent>
                            {selectedTask.description && (
                                <StyledInfoContent>
                                    <Typography variant="caption">{TranslateText('Description') + ':'}</Typography>
                                    <Typography variant="body_short">{selectedTask.description}</Typography>
                                </StyledInfoContent>
                            )}
                            {selectedTask.endTime && (
                                <StyledInfoContent>
                                    <Typography variant="caption">{TranslateText('Timestamp') + ':'}</Typography>
                                    <Typography variant="body_short">
                                        {formatDateTime(selectedTask.endTime, 'dd.MM.yy - HH:mm')}
                                    </Typography>
                                </StyledInfoContent>
                            )}
                        </StyledBottomContent>
                    </div>
                    <HiddenOnSmallScreen>
                        <InspectionOverviewDialogView tasks={tasks} />
                    </HiddenOnSmallScreen>
                </StyledDialogInspectionView>
            </StyledDialogContent>
        </StyledDialog>
    )
}
