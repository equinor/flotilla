import { Icon, Typography } from '@equinor/eds-core-react'
import { Task } from 'models/Task'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { Icons } from 'utils/icons'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { formatDateTime } from 'utils/StringFormatting'
import { useInspectionsContext } from 'components/Contexts/InpectionsContext'
import {
    StyledBottomContent,
    StyledCloseButton,
    StyledDialog,
    StyledDialogContent,
    StyledDialogHeader,
    StyledDialogInspectionView,
    StyledInfoContent,
    StyledInspection,
} from './InspectionStyles'
import { InspectionOverview } from './InspectionOverview'
import { fetchImageData } from './InspectionReportUtilities'

interface InspectionDialogViewProps {
    selectedTask: Task
    tasks: Task[]
}

export const InspectionDialogView = ({ selectedTask, tasks }: InspectionDialogViewProps) => {
    const { TranslateText } = useLanguageContext()
    const { installationName } = useInstallationContext()
    const { switchSelectedInspectionTask } = useInspectionsContext()
    const { data } = fetchImageData(selectedTask)

    const closeDialog = () => {
        switchSelectedInspectionTask(undefined)
    }

    return (
        <>
            {data !== undefined && (
                <StyledDialog open={true} isDismissable onClose={closeDialog}>
                    <StyledDialogContent>
                        <StyledDialogHeader>
                            <Typography variant="accordion_header" group="ui">
                                {TranslateText('Inspection report')}
                            </Typography>
                            <StyledCloseButton variant="ghost" onClick={closeDialog}>
                                <Icon name={Icons.Clear} size={24} />
                            </StyledCloseButton>
                        </StyledDialogHeader>
                        <StyledDialogInspectionView>
                            <div>
                                <StyledInspection src={data} />
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
                                            <Typography variant="caption">
                                                {TranslateText('Description') + ':'}
                                            </Typography>
                                            <Typography variant="body_short">{selectedTask.description}</Typography>
                                        </StyledInfoContent>
                                    )}
                                    {selectedTask.endTime && (
                                        <StyledInfoContent>
                                            <Typography variant="caption">
                                                {TranslateText('Timestamp') + ':'}
                                            </Typography>
                                            <Typography variant="body_short">
                                                {formatDateTime(selectedTask.endTime, 'dd.MM.yy - HH:mm')}
                                            </Typography>
                                        </StyledInfoContent>
                                    )}
                                </StyledBottomContent>
                            </div>
                            <InspectionOverview tasks={tasks} dialogView={true} />
                        </StyledDialogInspectionView>
                    </StyledDialogContent>
                </StyledDialog>
            )}
        </>
    )
}
