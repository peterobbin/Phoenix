<nav class="large-3 medium-4 columns" id="actions-sidebar">
    <ul class="side-nav">
        <li class="heading"><?= __('Actions') ?></li>
        <li><?= $this->Html->link(__('List Machines'), ['action' => 'index']) ?></li>
    </ul>
</nav>
<div class="machines form large-9 medium-8 columns content">
    <?= $this->Form->create($machine) ?>
    <fieldset>
        <legend><?= __('Add Machine') ?></legend>
        <?php
            echo $this->Form->input('name');
            echo $this->Form->input('public_key');
        ?>
    </fieldset>
    <?= $this->Form->button(__('Submit')) ?>
    <?= $this->Form->end() ?>
</div>
