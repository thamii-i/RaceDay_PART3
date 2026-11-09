(function () {
    function updateRoleSections() {
        var selected = document.querySelector('input[name="Role"]:checked');
        if (!selected) {
            return;
        }

        document.querySelectorAll('[data-role-section]').forEach(function (section) {
            var visible = section.getAttribute('data-role-section') === selected.value;
            section.style.display = visible ? '' : 'none';
        });
    }

    document.querySelectorAll('input[name="Role"]').forEach(function (radio) {
        radio.addEventListener('change', updateRoleSections);
    });

    updateRoleSections();
})();
