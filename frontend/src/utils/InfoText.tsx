import { Typography, Icon } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'

export const SmallScreenInfoText = () => {
    const { TranslateText } = useLanguageContext()
    return (
        <div id="SmallScreenInfoText">
            <Icon name={Icons.Info} size={24}></Icon>
            <Typography>{TranslateText('Small screen info text')}</Typography>
        </div>
    )
}
